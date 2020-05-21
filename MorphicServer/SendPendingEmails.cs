using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog.Context;

namespace MorphicServer
{
    public class SendGridSettings
    {
        /// <summary>
        /// The Sendgrid API key.
        /// 
        /// NOTE: Do not put this into any appsettings file. It's a secret and should be
        /// configured via environment variables
        ///
        /// For production: Put it in repo: deploy-morphiclite, path: environments/*/secrets/all.env
        /// depending on the environment
        ///
        /// For development and others, see launchSettings.json or the docker-compose.morphicserver.yml file.
        /// </summary>
        public string ApiKey { get; set; } = "";
    }
    
    public class EmailSettings
    {
        public static readonly string EmailTypeDisabled = "disabled";
        public static readonly string EmailTypeSendgrid = "sendgrid";
        public static readonly string EmailTypeLog = "log";
        
        /// <summary>
        /// The type of email sending we want. Supported: "sendgrid", "log", "disabled"
        /// </summary>
        public string Type { get; set; } = EmailTypeDisabled;
        
        public string EmailFromAddress { get; set; } = "support@morphic.world";
        /// <summary>
        /// The default 'name' we use to send emails.
        /// </summary>
        public string EmailFromFullname { get; set; } = "Morphic World Support";
        /// <summary>
        /// Some SendGrid settings.
        /// </summary>
        public SendGridSettings SendGridSettings { get; set; } = null!;
    }

    public class PendingEmail
    {
        // DB Fields
        public string UserId { get; set; }
        public string FromEmail { get; set; } = null!;
        public string FromFullName { get; set; } = null!;
        public string ToEmail { get; set; } = null!;
        public string ToFullName { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string EmailText { get; set; } = null!;
        public EmailTypeEnum EmailType { get; set; }
        
        public enum EmailTypeEnum
        {
            EmailValidation = 0
        }

        public PendingEmail(User user, string fromEmail, string fromFullName,
            string subject, string msg, EmailTypeEnum type)
        {
            UserId = user.Id;

            ToFullName = user.FullnameOrEmail();
            ToEmail = user.GetEmail() ?? throw new PendingEmailException("Email can not be null");
            EmailText = msg;
            Subject = subject;
            EmailType = type;
            FromEmail = fromEmail;
            FromFullName = fromFullName;
        }


        // Helpers
        
        public class PendingEmailException : MorphicServerException
        {
            public PendingEmailException(string error) : base(error)
            {
            }
        }
    }

    /// <summary>
    /// Should probably look into https://www.hangfire.io/ later. For now this should work
    /// https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html
    ///
    /// This is a background task that processes 'EmailsPerLoop' emails and then sleeps
    /// 'AfterLoopSleepSeconds' seconds. Emails aren't so time critical that we have to
    /// send them the millisecond we see them. Just process slowly and surely.
    ///
    /// The input to this job is a Mongo Collection that holds the pending emails. We
    /// use Mongo's FindOneAndUpdate() to get an entry, mark it as ours (by process ID)
    /// and when we grabbed it (so we can reset it to pending if we crash), and then send
    /// the email. If it fails, we reset the entry and someone else (or we) can try again later.
    ///
    /// Various TODO items sprinkled around in the code here.
    /// </summary>
    public class SendPendingEmails
    { 
        private readonly ILogger logger;
        private EmailSettings emailSettings;

        public SendPendingEmails(EmailSettings emailSettings, ILogger logger)
        {
            this.logger = logger;
            this.emailSettings = emailSettings;
        }
        
        private const string EmailSendingMetricHistogramName = "email_send_duration";
        private static readonly Histogram EmailSendingHistogram = Metrics.CreateHistogram(
            EmailSendingMetricHistogramName,
            "Time it takes to send an email",
            new[] {"type", "destination", "code"});

        public async Task SendOneEmail(PendingEmail pending)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeDisabled ||
                (emailSettings.Type == EmailSettings.EmailTypeSendgrid &&
                 (emailSettings.SendGridSettings == null || emailSettings.SendGridSettings.ApiKey == "")))
            {
                logger.LogError("Email sending disabled or misconfigured. Check EmailSettings.Type and " +
                                "SendGrid Api Key. Note this task can be retried manually from the Hangfire console");
                return;
            }

            logger.LogDebug("SendOneEmail processing PendingEmail");
            bool sent = false;
            if (emailSettings.Type == EmailSettings.EmailTypeSendgrid)
            {
                try
                {
                    sent = await SendViaSendGrid(pending);
                }
                catch (Exception e)
                {
                    logger.LogError($"SendViaSendGrid failed: {e}");
                }
            }
            else if (emailSettings.Type == EmailSettings.EmailTypeLog)
            {
                LogEmail(pending);
                sent = true;
            }

            if (!sent)
            {
                throw new UnableToSendEmailException();
            }
        }

        public class PendingEmailException : MorphicServerException
        {
        }
        
        public class UnableToSendEmailException : PendingEmailException
        {
            
        }
        /// <summary>
        /// Send one email via SendGrid.
        /// </summary>
        /// <param name="pending"></param>
        /// <returns></returns>
        private async Task<bool> SendViaSendGrid(PendingEmail pending)
        {
            var stopWatch = Stopwatch.StartNew();
            string code = "-500"; // a kind of combination of 500 error and unset (it's negative)
            try
            {
                var client = new SendGridClient(emailSettings.SendGridSettings.ApiKey);
                var from = new EmailAddress(pending.FromEmail, pending.FromFullName);
                var to = new EmailAddress(pending.ToEmail, pending.ToFullName);
                var msg = MailHelper.CreateSingleEmail(from, to,
                    pending.Subject, pending.EmailText, null);
                var response = await client.SendEmailAsync(msg);
                code = response.StatusCode.ToString();
                using (LogContext.PushProperty("StatusCode", response.StatusCode))
                using (LogContext.PushProperty("Headers", response.Headers))
                using (LogContext.PushProperty("Body", response.Body.ReadAsStringAsync().Result))
                {
                    if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.Ambiguous)
                    {
                        logger.LogError("Email send failed");
                        return false;
                    }
                    else
                    {
                        logger.LogDebug("Email send succeeded");
                        return true;
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                EmailSendingHistogram.Labels(pending.EmailType.ToString(), "sendgrid", code)
                    .Observe(stopWatch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Just log the email. Useful for debugging, dangerous for production (that's why we log it as WARNING).
        /// </summary>
        /// <param name="pending"></param>
        private void LogEmail(PendingEmail pending)
        {
            using (LogContext.PushProperty("FromEmail", $"{pending.FromEmail} <{pending.FromFullName}>"))
            using (LogContext.PushProperty("ToEmail", $"{pending.ToEmail} <{pending.ToFullName}>"))
            using (LogContext.PushProperty("Subject", $"{pending.Subject}"))
            using (LogContext.PushProperty("Text", $"{pending.EmailText}"))
                logger.LogWarning("Debug Email Logging");
        }
    }
}
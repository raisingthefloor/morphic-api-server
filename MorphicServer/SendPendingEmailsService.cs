using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Prometheus;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog.Context;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
        
        /// <summary>
        /// Number of seconds to sleep when first starting up.
        /// </summary>
        public int InitialSleepSecond { get; set; } = 5;
        /// <summary>
        /// Number of seconds to sleep at the end of each process loop.
        /// </summary>
        public int AfterLoopSleepSeconds { get; set; } = 5;
        /// <summary>
        /// Number of emails to process in each loop.
        /// </summary>
        public int EmailsPerLoop { get; set; } = 3;
        /// <summary>
        /// Even if we haven't processed EmailsPerLoop, stop (and log error) if we exceed this time.
        /// This is to guard against running each thread too long. We don't want to run too hard too often.
        /// (But does it matter?)
        /// </summary>
        public int MaxSecondsInLoop { get; set; } = 20;
        /// <summary>
        /// Number of minutes before we assume the process that grabbed this crashed and we reset it to run again.
        /// </summary>
        public int OrphanedPendingMinutes { get; set; } = 5;
        /// <summary>
        /// The default 'from:' address we send emails from.
        /// </summary>
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

    public class SendPendingEmailsService : BackgroundService
    { 
        private readonly ILogger logger;
        private EmailSettings emailSettings;
        private Database morphicDb;

        public SendPendingEmailsService(
            EmailSettings emailSettings,
            ILogger<SendPendingEmailsService> logger,
            Database morphicDb)
        {
            this.logger = logger;
            this.morphicDb = morphicDb;
            this.emailSettings = emailSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeDisabled)
            {
                logger.LogWarning($"EmailSettings.Type is set to {EmailSettings.EmailTypeDisabled}.");
                return;
            }

            if (emailSettings.Type == EmailSettings.EmailTypeSendgrid &&
                (emailSettings.SendGridSettings == null || emailSettings.SendGridSettings.ApiKey == ""))
            {
                logger.LogError($"EmailSettings.Type is set to {EmailSettings.EmailTypeDisabled} but SendGridSettings.ApiKey is empty");
                return;
            }

            // wait for things to settle down.
            await Task.Delay(emailSettings.InitialSleepSecond * 1000, stoppingToken);

            var processorId = $"{Process.GetCurrentProcess().Id}";
            using (LogContext.PushProperty("ProcessId", processorId))
            {
                logger.LogInformation("starting.");

                stoppingToken.Register(() =>
                    logger.LogInformation("stopping."));

                var maxLoopTimeSpan = new TimeSpan(0, 0, 0, emailSettings.MaxSecondsInLoop);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var topOfLoopTime = DateTime.UtcNow;
                    var maxLoops = emailSettings.EmailsPerLoop;
                    int emailCount = 0;
                    while (maxLoops > 0)
                    {
                        try
                        {
                            if (await FindAndProcessOnePendingEmail(processorId))
                            {
                                emailCount++;
                            }
                        }
                        catch (Exception e)
                        {
                            using (LogContext.PushProperty("Exception", e))
                            {
                                logger.LogError("Could not process pending email");
                            }
                        }

                        var timeInLoop = DateTime.UtcNow - topOfLoopTime;
                        if (timeInLoop >= maxLoopTimeSpan)
                        {
                            logger.LogInformation(
                                $"Max time in loop of {emailSettings.MaxSecondsInLoop}s exceeded. Processed {emailCount} emails");
                            break;
                        }

                        maxLoops--;
                    }
                    if (emailCount > 0) logger.LogDebug($"Processed email: {emailCount}");

                    await FindAndResetOrphanedPendingEmails();

                    // TODO Should link the stoppingToken to a token of our own, so we can programmatically kill the wait.
                    // This would allow us to set the timeout to much higher values (1 hour?) and wake up when there's
                    // emails to be sent from any of the endpoints.
                    await Task.Delay(emailSettings.AfterLoopSleepSeconds * 1000, stoppingToken);
                }

                logger.LogInformation("exiting.");
            }
        }

        private const string EmailSendingMetricHistogramName = "email_send_duration";
        private static readonly Histogram EmailSendingHistogram = Metrics.CreateHistogram(
            EmailSendingMetricHistogramName,
            "Time it takes to send an email",
            new[] {"type", "destination", "code"});

        private async Task<bool> FindAndProcessOnePendingEmail(string processorId)
        {
            var pending = await morphicDb.FindOneAndUpdate(
                p => p.ProcessorId == "",
                Builders<PendingEmail>.Update
                    .Set("ProcessorId", processorId)
                    .Set("Updated", DateTime.UtcNow)
            );
            if (pending != null)
            {
                using (LogContext.PushProperty("PendingEmail", pending.Id))
                {
                    logger.LogDebug("SendPendingEmailsService processing PendingEmail");
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
                    } else if (emailSettings.Type == EmailSettings.EmailTypeLog)
                    {
                        LogEmail(pending);
                    }

                    if (!sent)
                    {
                        logger.LogError("Could not send email");
                        // throw it back.
                        pending.ProcessorId = "";
                        await morphicDb.Save(pending);
                    }
                    else
                    {
                        logger.LogInformation("SendPendingEmailsService deleting PendingEmail");
                        await morphicDb.Delete(pending);
                    }

                    return true;
                }
            }

            return false;
        }

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
                        logger.LogError("Email send succeeded");
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

        private void LogEmail(PendingEmail pending)
        {
            using (LogContext.PushProperty("FromEmail", $"{pending.FromEmail} <{pending.FromFullName}>"))
            using (LogContext.PushProperty("ToEmail", $"{pending.ToEmail} <{pending.ToFullName}>"))
            using (LogContext.PushProperty("Subject", $"{pending.Subject}"))
                logger.LogWarning(pending.EmailText);
        }
        
        private async Task FindAndResetOrphanedPendingEmails()
        {
            var orphanedTime = DateTime.UtcNow - TimeSpan.FromMinutes(emailSettings.OrphanedPendingMinutes);
            var updated = await morphicDb.UpdateMany(
                p => p.ProcessorId != "" && p.Updated < orphanedTime,
                Builders<PendingEmail>.Update
                    .Set("ProcessorId", "")
            );
            if (updated > 0)
            {
                // this can happen if we crash during processing of some emails. Or perhap there's something
                // else wrong. In any case this should never happen, so log it as an error.
                logger.LogError($"FindAndResetOrphanedPendingEmails reset {updated} PendingEmails");
            }
        }
    }
}
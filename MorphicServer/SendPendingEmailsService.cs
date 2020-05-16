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
        public int AfterLoopSleepSeconds { get; set; } = 60;
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
                            await FindAndProcessOnePendingEmail(processorId);
                            emailCount++;
                        }
                        catch (NoPendingEmails)
                        {
                            break; // we can stop here.
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Could not process pending email. {Exception}", e);
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

        /// <summary>
        /// Find one email and reserve it: The tactic is to use Mongo's FindOneAndUpdate
        /// to find any PendingEmail with an empty ProcessId, write in OUR ProcessId, and
        /// then process it. This is guaranteed by mongo to be atomic, so should be a good
        /// locking mechanism.
        /// 
        /// Also set the updated timestamp so we can tell WHEN we reserved it. If we fail
        /// to process this (we crash?) then another check will free up the entry later.
        /// </summary>
        /// <param name="processorId"></param>
        /// <returns></returns>
        private async Task FindAndProcessOnePendingEmail(string processorId)
        {
            var now = DateTime.UtcNow;
            logger.LogDebug("Checking for pending email");
            var pending = await morphicDb.FindOneAndUpdate(
                p => p.ProcessorId == "" && p.SendAfter <= now,
                Builders<PendingEmail>.Update
                    .Set("ProcessorId", processorId)
                    .Set("Updated", DateTime.UtcNow)
            );
            if (pending == null)
            {
                throw new NoPendingEmails();
            }
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
                    sent = true;
                }

                if (!sent)
                {
                    logger.LogError("Could not send email");
                    // throw it back with exponential back-off.
                    pending.ProcessorId = "";
                    pending.Retries++;
                    pending.SendAfter = DateTime.UtcNow + TimeSpan.FromMinutes(1 * pending.Retries);
                    await morphicDb.Save(pending);
                }
                else
                {
                    logger.LogInformation("SendPendingEmailsService deleting PendingEmail");
                    await morphicDb.Delete(pending);
                }
            }
        }

        public class PendingEmailException : MorphicServerException
        {
        }

        public class NoPendingEmails : PendingEmailException
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

        /// <summary>
        /// Just log the email. Useful for debugging, dangerous for production (that's why we log it as WARNING).
        /// </summary>
        /// <param name="pending"></param>
        private void LogEmail(PendingEmail pending)
        {
            using (LogContext.PushProperty("FromEmail", $"{pending.FromEmail} <{pending.FromFullName}>"))
            using (LogContext.PushProperty("ToEmail", $"{pending.ToEmail} <{pending.ToFullName}>"))
            using (LogContext.PushProperty("Subject", $"{pending.Subject}"))
                logger.LogWarning(pending.EmailText);
        }
        
        /// <summary>
        /// Find all PendingEmails that have been 'reserved' (ProcessorId != "") and which
        /// have been sitting longer than they should ("OrphanedPendingMinutes", default 5 minutes
        /// when I wrote the code). Usually an email is grabbed and sent immediately, and if sendgrid
        /// is super slow, perhaps we time out (it's not clear what the default timeout is, but
        /// let's assume it's 30 seconds or 60 seconds or anyway something less than 5 minutes; perhaps
        /// we should set a timer/cancellation token ourselves?).
        /// TODO Figure out timeout situation
        /// </summary>
        /// <returns></returns>
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
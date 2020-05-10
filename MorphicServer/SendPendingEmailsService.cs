using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog.Context;

namespace MorphicServer
{
    public class SendGridSettings
    {
        public string ApiKey = "";
    }
    
    public class EmailSettings
    {
        public int SleepTimeSeconds = 5;
        public int EmailsPerLoop = 3;
        public int MaxSecondsInLoop = 10;
        public int OrphanedPendingMinutes = 30;
        public SendGridSettings? SendGridSettings;
        public string EmailFromAddress = "support@morphic.world";
        public string EmailFromFullname = "Morphic World Support";
    }

    public class SendPendingEmailsService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly EmailSettings settings;
        private readonly Database morphicDb;

        public SendPendingEmailsService(MorphicSettings settings,
            ILogger<SendPendingEmailsService> logger, Database morphicDb)
        {
            this.logger = logger;
            this.morphicDb = morphicDb;
            this.settings = settings.EmailSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // wait 30 seconds for things to settle down.
            await Task.Delay(30 * 1000, stoppingToken);

            var processorId = $"{Process.GetCurrentProcess().Id}";
            using (LogContext.PushProperty("ProcessId", processorId))
            {
                logger.LogInformation("starting.");

                stoppingToken.Register(() =>
                    logger.LogInformation("stopping."));

                var maxLoopTimeSpan = new TimeSpan(0, 0, 0, settings.MaxSecondsInLoop);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var topOfLoopTime = DateTime.UtcNow;
                    var maxLoops = settings.EmailsPerLoop;
                    int emailCount = 0;
                    while (maxLoops > 0)
                    {
                        if (await FindAndProcessOnePendingEmail(processorId))
                        {
                            emailCount++;
                        }

                        var timeInLoop = DateTime.UtcNow - topOfLoopTime;
                        if (timeInLoop >= maxLoopTimeSpan)
                        {
                            logger.LogInformation(
                                $"Max time in loop of {settings.MaxSecondsInLoop}s exceeded. Processed {emailCount} emails");
                            break;
                        }

                        maxLoops--;
                    }
                    if (emailCount > 0) logger.LogDebug($"Processed email: {emailCount}");

                    await FindAndResetOrphanedPendingEmails();

                    // TODO Should link the stoppingToken to a token of our own, so we can programmatically kill the wait.
                    // This would allow us to set the timeout to much higher values (1 hour?) and wake up when there's
                    // emails to be sent from any of the endpoints.
                    await Task.Delay(settings.SleepTimeSeconds * 1000, stoppingToken);
                }

                logger.LogInformation("exiting.");
            }
        }

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
                    var sent = await SendViaSendGrid(pending);
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
            var client = new SendGridClient(settings.SendGridSettings!.ApiKey);
            var to = new EmailAddress(settings.EmailFromAddress, settings.EmailFromFullname);

            var from = new EmailAddress(pending.ToEmail, pending.ToFullName);
            var msg = MailHelper.CreateSingleEmail(from, to, pending.Subject, pending.EmailText, null);
            var response = await client.SendEmailAsync(msg);
            logger.LogDebug($"Email result: {response.StatusCode} {response.Body.ReadAsStringAsync().Result} {response.Headers.ToString()}");
            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.Ambiguous)
            {
                return false;
            }
            return true;
        }

        private async Task FindAndResetOrphanedPendingEmails()
        {
            var orphanedTime = DateTime.UtcNow - TimeSpan.FromMinutes(settings.OrphanedPendingMinutes);
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
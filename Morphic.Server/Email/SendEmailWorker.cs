using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Email
{
    public abstract class SendEmailWorker
    {
        protected readonly ILogger logger;
        protected readonly EmailSettings emailSettings;

        public SendEmailWorker(EmailSettings emailSettings, ILogger logger)
        {
            this.emailSettings = emailSettings;
            this.logger = logger;
        }

        public abstract Task<bool> SendTemplate(EmailConstants.EmailTypes emailType,
            Dictionary<string, string> emailAttributes);

    }

    public static class SendEmailWorkerFactory
    {
        public static SendEmailWorker? Get(EmailSettings emailSettings, ILogger logger)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeSendgrid)
            {
                return new Sendgrid(emailSettings, logger);
            }
            if (emailSettings.Type == EmailSettings.EmailTypeSendInBlue)
            {
                return new SendInBlue(emailSettings, logger);
            }
            if (emailSettings.Type == EmailSettings.EmailTypeLog)
            {
                return new EmailLogger(emailSettings, logger);
            }
            return null;
        }
    }
}
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
}
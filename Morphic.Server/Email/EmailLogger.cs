using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Email
{
    public class EmailLogger : SendEmailWorker
    {
        public EmailLogger(EmailSettings emailSettings, ILogger logger) : base(emailSettings, logger)
        {
        }

        public override async Task<bool> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            return await Task.Run(() =>
            {
                // NOTE: This is a WARNING because if we accidentally use this Worker in production, that's a big problem.
                logger.LogWarning("Debug Email Logging {EmailType}, {Attributes}",
                    emailType, string.Join(" ", emailAttributes));
                return true;
            });
        }
    }
}
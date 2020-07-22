using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Morphic.Server.Email
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

        public string WelcomeEmailValidationId { get; set; } = "";
        public string PasswordResetId { get; set; } = "";
        public string PasswordResetEmailNotValidatedId { get; set; } = "";
        public string PasswordResetUnknownEmailId { get; set; } = "";
        public string ChangePasswordEmailId { get; set; } = "";
        public string CommunityInvitationId { get; set; } = "";
    }

    public class Sendgrid : SendEmailWorker
    {
        public Sendgrid(EmailSettings emailSettings, ILogger logger) : base(emailSettings, logger)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeSendgrid &&
                (emailSettings.SendGridSettings == null || emailSettings.SendGridSettings.ApiKey == ""))
            {
                throw new SendgridException("misconfigured settings");
            }
        }

        private string EmailTypeToSendgridId(EmailConstants.EmailTypes emailType)
        {
            string emailTemplateId = "";
            switch (emailType)
            {
                case EmailConstants.EmailTypes.PasswordReset:
                    emailTemplateId = emailSettings.SendGridSettings.PasswordResetId;
                    break;
                case EmailConstants.EmailTypes.WelcomeEmailValidation:
                    emailTemplateId = emailSettings.SendGridSettings.WelcomeEmailValidationId;
                    break;
                case EmailConstants.EmailTypes.PasswordResetUnknownEmail:
                    emailTemplateId = emailSettings.SendGridSettings.PasswordResetUnknownEmailId;
                    break;
                case EmailConstants.EmailTypes.PasswordResetEmailNotValidated:
                    emailTemplateId = emailSettings.SendGridSettings.PasswordResetEmailNotValidatedId;
                    break;
                case EmailConstants.EmailTypes.ChangePasswordEmail:
                    emailTemplateId = emailSettings.SendGridSettings.ChangePasswordEmailId;
                    break;
                case EmailConstants.EmailTypes.CommunityInvitation:
                    emailTemplateId = emailSettings.SendGridSettings.CommunityInvitationId;
                    break;
                case EmailConstants.EmailTypes.None:
                    throw new SendgridException("EmailType None");
            }

            if (emailTemplateId == "")
            {
                throw new SendgridException("EmailType Unknown: " + emailType.ToString());
            }

            return emailTemplateId;
        }

        public override async Task<bool> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            var emailTemplateId = EmailTypeToSendgridId(emailType);
            var from = new EmailAddress(emailAttributes["FromEmail"], emailAttributes["FromUserName"]);
            var to = new EmailAddress(emailAttributes["ToEmail"], emailAttributes["ToUserName"]);
            var msg = MailHelper.CreateSingleTemplateEmail(from, to, emailTemplateId, emailAttributes);
            return await SendViaSendGrid(msg);
        }

        private async Task<bool> SendViaSendGrid(SendGridMessage msg)
        {
            var client = new SendGridClient(emailSettings.SendGridSettings.ApiKey);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.Ambiguous)
            {
                logger.LogError("Email send failed: {StatusCode} {Headers} {Body}", response.StatusCode,
                    response.Headers, response.Body.ReadAsStringAsync().Result);
                return false;
            }
            else
            {
                logger.LogDebug("Email send succeeded: {StatusCode} {Headers} {Body}", response.StatusCode,
                    response.Headers, response.Body.ReadAsStringAsync().Result);
                return true;
            }
        }
        
        class SendgridException : MorphicServerException
        {
            public SendgridException(string error) : base(error)
            {
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace Morphic.Server.Email
{
    public class SendInBlueSettings
    {
        public string ApiKey { get; set; } = "";
        public string WelcomeEmailValidationId { get; set; } = "1";
        public string PasswordResetId { get; set; } = "";
        public string PasswordResetEmailNotValidatedId { get; set; } = "";
        public string PasswordResetUnknownEmailId { get; set; } = "";
    }

    public class SendInBlue : SendEmailWorker
    {
        public SendInBlue(EmailSettings emailSettings, ILogger logger) : base(emailSettings, logger)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeSendInBlue &&
                (emailSettings.SendInBlueSettings == null || emailSettings.SendInBlueSettings.ApiKey == ""))
            {
                throw new SendInBlueException("misconfigured settings");
            }
        }

        private long EmailTypeToSendInBlueId(EmailConstants.EmailTypes emailType)
        {
            string emailTemplateId = "";
            switch (emailType)
            {
                case EmailConstants.EmailTypes.PasswordReset:
                    emailTemplateId = emailSettings.SendInBlueSettings.PasswordResetId;
                    break;
                case EmailConstants.EmailTypes.WelcomeEmailValidation:
                    emailTemplateId = emailSettings.SendInBlueSettings.WelcomeEmailValidationId;
                    break;
                case EmailConstants.EmailTypes.PasswordResetUnknownEmail:
                    emailTemplateId = emailSettings.SendInBlueSettings.PasswordResetUnknownEmailId;
                    break;
                case EmailConstants.EmailTypes.PasswordResetEmailNotValidated:
                    emailTemplateId = emailSettings.SendInBlueSettings.PasswordResetEmailNotValidatedId;
                    break;
                case EmailConstants.EmailTypes.None:
                    throw new SendInBlueException("EmailType None");
            }

            if (emailTemplateId == "")
            {
                throw new SendInBlueException("EmailType Unknown: " + emailType.ToString());
            }

            try
            {
                return Convert.ToInt64(emailTemplateId);
            }
            catch (Exception e)
            {
                throw new SendInBlueException(string.Format("EmailType {}: Could not convert {} to long. {}",
                    emailType, emailTemplateId, e.ToString()));
            }
        }
        
        public override async Task<bool> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            var to = new List<SendSmtpEmailTo>();
            to.Add(new SendSmtpEmailTo(emailAttributes["ToEmail"], emailAttributes["ToUserName"]));
            var sendSmtpEmail = new SendSmtpEmail(to: to);
            sendSmtpEmail.TemplateId = EmailTypeToSendInBlueId(emailType);
            sendSmtpEmail.Params = emailAttributes;
            return await SendViaSendInBlue(sendSmtpEmail);
        }
        
        private async Task<bool> SendViaSendInBlue(SendSmtpEmail msg)
        {
            var configuration = new Configuration();
            configuration.AddApiKey("api-key", emailSettings.SendInBlueSettings.ApiKey);
            var apiInstance = new SMTPApi(configuration);

            try
            {
                // Send a transactional email
                CreateSmtpEmail result = await apiInstance.SendTransacEmailAsync(msg);
                logger.LogDebug("SendInBlue sent email {Result}", result);
                return true;
            }
            catch (Exception e)
            {
                throw new SendInBlueException("Exception when calling SMTPApi.SendTransacEmail: " + e.Message );
            }
        }
        
        class SendInBlueException : MorphicServerException
        {
            public SendInBlueException(string error) : base(error)
            {
            }
        }
    }
}
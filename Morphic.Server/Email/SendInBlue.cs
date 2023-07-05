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
        public string ApiKey
        {
            get
            {
                var apiKey = Morphic.Server.Settings.MorphicAppSecret.GetSecret("api-server", "EMAILSETTINGS__SENDINBLUESETTINGS__APIKEY") ?? "";
                return apiKey;
            }
        }
        
        public string WelcomeEmailValidationId { get; set; } = "";
        public string PasswordResetId { get; set; } = "";
        public string PasswordResetEmailNotValidatedId { get; set; } = "";
        public string PasswordResetUnknownEmailId { get; set; } = "";
        public string ChangePasswordEmailId { get; set; } = "";
        public string CommunityInvitationId { get; set; } = "";
        public string CommunityInvitationManagerId { get; set; } = "";
    }

    /// <summary>
    /// Send emails via SendInBlue Transactional templates.
    ///
    /// For customizing templates, see https://help.sendinblue.com/hc/en-us/articles/360000946299-Create-customize-transactional-email-templates
    /// </summary>
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
                case EmailConstants.EmailTypes.ChangePasswordEmail:
                    emailTemplateId = emailSettings.SendInBlueSettings.ChangePasswordEmailId;
                    break;
                case EmailConstants.EmailTypes.CommunityInvitation:
                    emailTemplateId = emailSettings.SendInBlueSettings.CommunityInvitationId;
                    break;
                case EmailConstants.EmailTypes.CommunityInvitationManager:
                    emailTemplateId = emailSettings.SendInBlueSettings.CommunityInvitationManagerId;
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
        
        public override async Task<string> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            var to = new List<SendSmtpEmailTo>();
            to.Add(new SendSmtpEmailTo(emailAttributes["ToEmail"], emailAttributes["ToUserName"]));
            var sendSmtpEmail = new SendSmtpEmail(to: to);
            sendSmtpEmail.TemplateId = EmailTypeToSendInBlueId(emailType);
            sendSmtpEmail.Params = emailAttributes;
            return await SendViaSendInBlue(sendSmtpEmail);
        }
        
        private async Task<string> SendViaSendInBlue(SendSmtpEmail msg)
        {
            var configuration = new Configuration();
            configuration.AddApiKey("api-key", emailSettings.SendInBlueSettings.ApiKey);
            var apiInstance = new SMTPApi(configuration);

            try
            {
                // Send a transactional email
                CreateSmtpEmail result = await apiInstance.SendTransacEmailAsync(msg);
                logger.LogDebug("SendInBlue sent email {Result}", result.MessageId);
                return result.MessageId;
            }
            catch (Exception e)
            {
                throw new SendInBlueException("Exception when calling SMTPApi.SendTransacEmailAsync: ", e);
            }
        }
        
        class SendInBlueException : MorphicServerException
        {
            public SendInBlueException(string error, Exception exception) : base(error, exception)
            {
            }
            public SendInBlueException(string error) : base(error)
            {
            }
        }
    }
}
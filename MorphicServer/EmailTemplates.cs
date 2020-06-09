// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace MorphicServer
{
    /// <summary>
    /// Class for Pending Emails.
    /// </summary>

    public abstract class EmailTemplates: BackgroundJob
    {
        // https://github.com/sendgrid/sendgrid-csharp/blob/master/USE_CASES.md#transactional-templates
        // TODO i18n? localization?
        // TODO Should the templates themselves live in the DB for easier updating (and easier localization)?

        // TODO For tracking and user inquiries, we should have an audit log.
        // Things we might need to know (i.e. things customers may call about):
        // . "I didn't request this email!" -> Source IP? Username/email? What else can we know?
        // . "I didn't get my email." -> Sendgrid logs? Do we need our own?

        /// <summary>
        /// The DB reference
        /// </summary>
        protected readonly Database Db;

        /// <summary>
        /// The Email Settings
        /// </summary>
        protected readonly EmailSettings EmailSettings;
        
        protected readonly ILogger logger;

        protected const string UnknownClientIp = "Unknown Client Ip";

        /// <summary>
        /// Using this is kind of ugly: Knowledge of the keys and their data is shared
        /// by this class and the SendEmail class. Should probably find a nicer way, that's
        /// also flexible.
        /// </summary>
        protected Dictionary<string, string> Attributes;
        
        protected EmailTemplates(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailTemplates> logger, Database db): base(morphicSettings)
        {
            Db = db;
            EmailSettings = settings;
            this.logger = logger;
            Attributes = new Dictionary<string, string>();
        }

        protected string EmailTemplateId = "";
        protected string EmailType = "";

        /// <summary>
        /// Caller is expected to make sure user.Email.Plaintext is not null.
        ///
        /// </summary>
        /// <param name="user"></param>
        /// <param name="link"></param>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        protected void FillAttributes(User user, string? link, string? clientIp)
        {
            Attributes.Add("EmailType", EmailType);
            Attributes.Add("ToUserName", user.FullnameOrEmail());
            Attributes.Add("ToEmail", user.Email.PlainText!);
            Attributes.Add("FromUserName", EmailSettings.EmailFromFullname);
            Attributes.Add("FromEmail", EmailSettings.EmailFromAddress);
            Attributes.Add("ClientIp", clientIp ?? UnknownClientIp);
            Attributes.Add("Link", link ?? "");
        }
    }

    class EmailTemplatesException : MorphicServerException
    {
        public EmailTemplatesException(string error) : base(error)
        {
        }
    }
    public class EmailVerificationEmail : EmailTemplates
    {
        public EmailVerificationEmail(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailVerificationEmail> logger, Database db) : base(morphicSettings, settings, logger, db)
        {
            EmailType = "EmailValidation";
            EmailTemplateId = "d-aecd70a619eb49deadbb74451797dd04";
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task SendEmail(string userId, string? clientIp)
        {
            if (EmailSettings.Type == EmailSettings.EmailTypeDisabled)
            {
                // Email shouldn't be disabled, but if it is, we want to
                // fail this job so it retries
                throw new Exception("Email is disabled, failing job to it will retry");
            }
            var user = await Db.Get<User>(userId);
            if (user == null)
            {
                throw new EmailTemplatesException("No User");
            }
            if (user.Email.PlainText == null)
            {
                logger.LogDebug($"Sending email to user {user.Id} who doesn't have an email address");
                return;
            }
            var oneTimeToken = new OneTimeToken(user.Id);
            var verifyUri = GetFrontEndUri("/email/verify", new Dictionary<string, string>()
            {
                {"user_id", oneTimeToken.UserId},
                {"token", oneTimeToken.GetUnhashedToken()}
            });
            await Db.Save(oneTimeToken);

            FillAttributes(user, verifyUri.ToString(), clientIp);
            await new SendEmail(EmailSettings, logger).SendOneEmail(EmailTemplateId, Attributes);
        }
    }

    public class PasswordResetEmail : EmailTemplates
    {
        public PasswordResetEmail(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailVerificationEmail> logger, Database db) : base(morphicSettings, settings, logger, db)
        {
            EmailType = "PasswordResetExistingUser";
            EmailTemplateId = "d-8dbb4d6c44cb423f8bef356b832246bd";
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task SendEmail(string userId, string? clientIp)
        {
            if (EmailSettings.Type == EmailSettings.EmailTypeDisabled)
            {
                // Email shouldn't be disabled, but if it is, we want to
                // fail this job so it retries
                throw new Exception("Email is disabled, failing job to it will retry");
            }

            var user = await Db.Get<User>(userId);
            if (user == null)
            {
                throw new EmailTemplatesException("No User");
            }
            if (user.Email.PlainText == null)
            {
                logger.LogDebug($"Sending email to user {user.Id} who doesn't have an email address");
                return;
            }

            var oneTimeToken = new OneTimeToken(user.Id);
            var uri = GetFrontEndUri("/password/reset", new Dictionary<string, string>()
            {
                {"token", oneTimeToken.GetUnhashedToken()}
            });
            await Db.Save(oneTimeToken);
            FillAttributes(user, uri.ToString(), clientIp);
            await new SendEmail(EmailSettings, logger).SendOneEmail(EmailTemplateId, Attributes);
        }
    }
    
    public class UnknownEmailPasswordResetEmail : EmailTemplates
    {
        public UnknownEmailPasswordResetEmail(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailVerificationEmail> logger, Database db) : base(morphicSettings, settings, logger, db)
        {
            EmailType = "PasswordResetNoUser";
            EmailTemplateId = "d-ea0b8cbcb7524f58a385b7dc02cde30a";
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task SendEmail(string destinationEmail, string? clientIp)
        {
            if (EmailSettings.Type == EmailSettings.EmailTypeDisabled)
            {
                // Email shouldn't be disabled, but if it is, we want to
                // fail this job so it retries
                throw new Exception("Email is disabled, failing job to it will retry");
            }

            // Don't save this. It's just to carry the email
            var user = new User();
            user.Email.PlainText = destinationEmail;
            FillAttributes(user, null, clientIp);
            await new SendEmail(EmailSettings, logger).SendOneEmail(EmailTemplateId, Attributes);
        }
    }
    
    public class EmailNotVerifiedPasswordResetEmail : EmailTemplates
    {
        public EmailNotVerifiedPasswordResetEmail(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailVerificationEmail> logger, Database db) : base(morphicSettings, settings, logger, db)
        {
            EmailType = "PasswordResetEmailNotVerified";
            EmailTemplateId = "d-1d54234ca083487c8a14e3fba27c9e6a";
        }

        [AutomaticRetry(Attempts = 20)]
        public async Task SendEmail(string destinationEmail, string? clientIp)
        {
            if (EmailSettings.Type == EmailSettings.EmailTypeDisabled)
            {
                // Email shouldn't be disabled, but if it is, we want to
                // fail this job so it retries
                throw new Exception("Email is disabled, failing job to it will retry");
            }

            // Don't save this. It's just to carry the email
            var user = new User();
            user.Email.PlainText = destinationEmail;
            FillAttributes(user, null, clientIp);
            await new SendEmail(EmailSettings, logger).SendOneEmail(EmailTemplateId, Attributes);
        }
    }
}
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

using System.Threading.Tasks;
using Antlr3.ST;
using Hangfire;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MorphicServer
{
    /// <summary>
    /// Class for Pending Emails.
    ///
    /// TODO Need some retry-counter and/or retry timer so we don't retry X times directly in a row.
    /// </summary>

    public abstract class EmailTemplates
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
        protected Database Db;

        /// <summary>
        /// The Email Settings
        /// </summary>
        protected EmailSettings Settings;
        
        protected readonly ILogger logger;

        
        protected EmailTemplates(EmailSettings settings, ILogger<EmailTemplates> logger, Database db)
        {
            Db = db;
            Settings = settings;
            this.logger = logger;
        }
    }

    class EmailTemplatesException : MorphicServerException
    {
        public EmailTemplatesException(string error) : base(error)
        {
        }
    }
    public class NewVerificationEmail : EmailTemplates
    {
        private const string EmailVerificationMsgTemplate =
            @"Dear $UserFullName$,

To verify your email address $UserEmail$ please click the following link: $Link$

Regards,
$MorphicUser$ ($MorphicEmail$)";
        
        public NewVerificationEmail(EmailSettings settings, ILogger<NewVerificationEmail> logger, Database db) : base(settings, logger, db)
        {
        }

        protected async Task<PendingEmail> CreatePendingEmail(User user, string urlTemplate)
        {
            var oneTimeToken = new OneTimeToken(user.Id);

            // Create the email message
            var link = urlTemplate
                .Replace("{oneTimeToken}", oneTimeToken.GetUnhashedToken())
                .Replace("{userId}", oneTimeToken.UserId);
            StringTemplate emailVerificationMsg = new StringTemplate(EmailVerificationMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", user.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", user.GetEmail());
            emailVerificationMsg.SetAttribute("Link", link);
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(user, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Email Verification", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            await Db.Save(oneTimeToken);
            return pending;
        }
        
        [AutomaticRetry(Attempts = 20)]
        public async Task QueueEmail(string userId, string urlTemplate)
        {
            if (Settings.Type == EmailSettings.EmailTypeDisabled)
            {
                Log.Logger.Warning("EmailSettings.Disable is set");
                return;
            }

            var user = await Db.Get<User>(userId);
            if (user == null)
            {
                throw new EmailTemplatesException("No User");
            }
            if (user.GetEmail() == null)
            {
                Log.Logger.Debug($"Sending email to user {user.Id} who doesn't have an email address");
                return;
            }

            var pending = await CreatePendingEmail(user, urlTemplate);
            await new SendPendingEmails(Settings, logger).SendOneEmail(pending);
        }
    }

    public class NewPasswordResetEmail : EmailTemplates
    {
        private const string PasswordResetLinkMsgTemplate =
            @"Dear $UserFullName$,

Someone requested a password reset for this email address. If this wasn't you, you may ignore
this email or contact Morphic Support.

To reset your password, please click the following link and follow the instructions: $Link$

Regards,
$MorphicUser$ ($MorphicEmail$)";
        
        public NewPasswordResetEmail(EmailSettings settings, ILogger<NewVerificationEmail> logger, Database db) : base(settings, logger, db)
        {
        }

        protected async Task<PendingEmail> CreatePendingEmail(User user, string urlTemplate)
        {
            var oneTimeToken = new OneTimeToken(user.Id);

            // Create the email message
            var link = urlTemplate.Replace("{oneTimeToken}", oneTimeToken.GetUnhashedToken());
            StringTemplate emailVerificationMsg = new StringTemplate(PasswordResetLinkMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", user.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", user.GetEmail());
            emailVerificationMsg.SetAttribute("Link", link);
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(user, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Password Reset", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            await Db.Save(oneTimeToken);
            return pending;
        }
        
        [AutomaticRetry(Attempts = 20)]
        public async Task QueueEmail(string userId, string urlTemplate)
        {
            if (Settings.Type == EmailSettings.EmailTypeDisabled)
            {
                Log.Logger.Warning("EmailSettings.Disable is set");
                return;
            }

            var user = await Db.Get<User>(userId);
            if (user == null)
            {
                throw new EmailTemplatesException("No User");
            }
            if (user.GetEmail() == null)
            {
                Log.Logger.Debug($"Sending email to user {user.Id} who doesn't have an email address");
                return;
            }

            var pending = await CreatePendingEmail(user, urlTemplate);
            await new SendPendingEmails(Settings, logger).SendOneEmail(pending);
        }
    }
    
    public class NewNoEmailPasswordResetEmail : EmailTemplates
    {
        private const string PasswordResetNoEmailMsgTemplate =
            @"Dear $UserFullName$,

Someone requested a password reset for this email address. However no account exists for this
email. If this wasn't requested by you, you may ignore this email or contact Morphic Support.

Regards,
$MorphicUser$ ($MorphicEmail$)";

        public NewNoEmailPasswordResetEmail(EmailSettings settings, ILogger<NewVerificationEmail> logger, Database db) : base(settings, logger, db)
        {
        }

        protected async Task<PendingEmail> CreatePendingEmail(User user)
        {
            // Create the email message
            StringTemplate emailVerificationMsg = new StringTemplate(PasswordResetNoEmailMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", user.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", user.GetEmail());
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(user, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Password Reset", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            return pending;
        }
        [AutomaticRetry(Attempts = 20)]
        public async Task QueueEmail(string destinationEmail)
        {
            if (Settings.Type == EmailSettings.EmailTypeDisabled)
            {
                Log.Logger.Warning("EmailSettings.Disable is set");
                return;
            }

            // Don't save this. Just to carry the email
            var user = new User();
            user.SetEmail(destinationEmail);
            
            var pending = await CreatePendingEmail(user);
            await new SendPendingEmails(Settings, logger).SendOneEmail(pending);
        }
    }
}
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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Antlr3.ST;
using Hangfire;
using MongoDB.Bson.Serialization.Attributes;
using Serilog;

namespace MorphicServer
{
    /// <summary>
    /// Class for Pending Emails.
    ///
    /// TODO Need some retry-counter and/or retry timer so we don't retry X times directly in a row.
    /// </summary>
    public class PendingEmail : Record
    {
        // DB Fields
        public string UserId { get; set; }
        public string FromEmail { get; set; } = null!;
        public string FromFullName { get; set; } = null!;
        public string ToEmailEncr { get; set; } = null!;
        public string ToFullNameEncr { get; set; } = null!;
        public string SubjectEncr { get; set; } = null!;
        public string EmailTextEncr { get; set; } = null!;
        public EmailTypeEnum EmailType { get; set; }
        
        public enum EmailTypeEnum
        {
            EmailValidation = 0
        }

        public PendingEmail(User user, string fromEmail, string fromFullName,
            string subject, string msg, EmailTypeEnum type)
        {
            Id = Guid.NewGuid().ToString();
            UserId = user.Id;

            ToFullName = user.FullnameOrEmail();
            ToEmail = user.GetEmail() ?? throw new PendingEmailException("Email can not be null");
            EmailText = msg;
            Subject = subject;
            EmailType = type;
            FromEmail = fromEmail;
            FromFullName = fromFullName;
        }


        // Helpers
        
        private string? toEmail;
        [BsonIgnore]
        [JsonIgnore]
        public string ToEmail
        {
            get
            {
                if (toEmail == null)
                {
                    toEmail = EncryptedField.FromCombinedString(ToEmailEncr).Decrypt();
                }
                return toEmail;
            }
            
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new PendingEmailException("Empty or null ToEmail");
                }
                ToEmailEncr = EncryptedField.FromPlainText(value).ToCombinedString();
                toEmail = value;
            }
        }

        private string? toFullName;
        [BsonIgnore]
        [JsonIgnore]
        public string ToFullName
        {
            get
            {
                if (toFullName == null)
                {
                    toFullName = ToFullNameEncr != "" ? EncryptedField.FromCombinedString(ToFullNameEncr).Decrypt() : "";
                }
                return toFullName;
            }
            
            set
            {
                ToFullNameEncr = value != "" ? EncryptedField.FromPlainText(value).ToCombinedString() : "";
                toFullName = value;
            }
        }

        private string? subject;
        [BsonIgnore]
        [JsonIgnore]
        public string Subject
        {
            get
            {
                if (subject == null)
                {
                    subject = EncryptedField.FromCombinedString(SubjectEncr).Decrypt();
                }
                return subject;
            }
            
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new PendingEmailException("Empty or null Subject");
                }

                SubjectEncr = EncryptedField.FromPlainText(value).ToCombinedString();
                subject = value;
            }
        }

        private string? emailText;
        [BsonIgnore]
        [JsonIgnore]
        public string EmailText
        {
            get
            {
                if (emailText == null)
                {
                    emailText = EncryptedField.FromCombinedString(EmailTextEncr).Decrypt();
                }
                return emailText;
            }
            
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new PendingEmailException("Empty or null EmailText");
                }

                EmailTextEncr = EncryptedField.FromPlainText(value).ToCombinedString();
                emailText = value;
            }
        }
        
        public class PendingEmailException : MorphicServerException
        {
            public PendingEmailException(string error) : base(error)
            {
            }

            public PendingEmailException() : base()
            {
            }

        }
    }

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

        /// <summary>
        /// The user to send the email to
        /// </summary>
        protected User? User = null;

        protected EmailTemplates(EmailSettings settings, Database db, User? user)
        {
            Db = db;
            Settings = settings;
            User = user;
        }

        public async Task QueueEmail()
        {
            if (Settings.Type == EmailSettings.EmailTypeDisabled)
            {
                Log.Logger.Warning("EmailSettings.Disable is set");
                return;
            }

            if (User == null)
            {
                throw new EmailTemplatesException("No User");
            }
            if (User.GetEmail() == null)
            {
                Log.Logger.Debug($"Sending email to user {User.Id} who doesn't have an email address");
                return;
            }

            var pending = await CreatePendingEmail();
            BackgroundJob.Enqueue<SendPendingEmailsService>(x => x.SendOneEmail(pending.Id));
        }

        protected abstract Task<PendingEmail> CreatePendingEmail();
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
        /// <summary>
        /// The urlTemplate used to create some kind of link the user needs to follow in the email
        /// </summary>
        protected string UrlTemplate;
        
        public NewVerificationEmail(EmailSettings settings, Database db, User user, string urlTemplate) : base(settings, db, user)
        {
            UrlTemplate = urlTemplate;
        }

        protected override async Task<PendingEmail> CreatePendingEmail()
        {
            var oneTimeToken = new OneTimeToken(User!.Id);

            // Create the email message
            var link = UrlTemplate
                .Replace("{oneTimeToken}", oneTimeToken.GetUnhashedToken())
                .Replace("{userId}", oneTimeToken.UserId);
            StringTemplate emailVerificationMsg = new StringTemplate(EmailVerificationMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", User.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", User.GetEmail());
            emailVerificationMsg.SetAttribute("Link", link);
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(User, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Email Verification", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            await Db.Save(oneTimeToken);
            await Db.Save(pending);
            return pending;
        }
    }

    public class NewPasswordResetEmail : EmailTemplates
    {
        private const string PasswordResetLinkMsgTemplate =
            @"Dear $UserFullName$,

Someone requested a password reset for this email address. If this wasn't you, you may ignore
this email or contact Morphic Support.

To reset your password, please click the following link: $Link$

Regards,
$MorphicUser$ ($MorphicEmail$)";

        protected string UrlTemplate;

        public NewPasswordResetEmail(EmailSettings settings, Database db, User user, string urlTemplate) : base(settings, db, user)
        {
            UrlTemplate = urlTemplate;
        }

        protected override async Task<PendingEmail> CreatePendingEmail()
        {
            var oneTimeToken = new OneTimeToken(User!.Id);

            // Create the email message
            var link = UrlTemplate.Replace("{oneTimeToken}", oneTimeToken.GetUnhashedToken());
            StringTemplate emailVerificationMsg = new StringTemplate(PasswordResetLinkMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", User.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", User.GetEmail());
            emailVerificationMsg.SetAttribute("Link", link);
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(User, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Password Reset", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            await Db.Save(oneTimeToken);
            await Db.Save(pending);
            return pending;
        }
    }
    
    public class NewNoPasswordResetEmail : EmailTemplates
    {
        private const string PasswordResetNoEmailMsgTemplate =
            @"Dear $UserFullName$,

Someone requested a password reset for this email address. However no account exists for this
email. If this wasn't requested by you, you may ignore this email or contact Morphic Support.

Regards,
$MorphicUser$ ($MorphicEmail$)";

        public NewNoPasswordResetEmail(EmailSettings settings, Database db, string destinationEmail) : base(settings, db, null)
        {
            // Don't save this. Just to carry the email
            User = new User();
            User.SetEmail(destinationEmail);
        }

        protected override async Task<PendingEmail> CreatePendingEmail()
        {
            // Create the email message
            StringTemplate emailVerificationMsg = new StringTemplate(PasswordResetNoEmailMsgTemplate);
            emailVerificationMsg.SetAttribute("UserFullName", User!.FullnameOrEmail());
            emailVerificationMsg.SetAttribute("UserEmail", User!.GetEmail());
            emailVerificationMsg.SetAttribute("MorphicUser", Settings.EmailFromFullname);
            emailVerificationMsg.SetAttribute("MorphicEmail", Settings.EmailFromAddress);

            var pending = new PendingEmail(User, Settings.EmailFromAddress, Settings.EmailFromFullname,
                "Password Reset", emailVerificationMsg.ToString(),
                PendingEmail.EmailTypeEnum.EmailValidation);
            await Db.Save(pending);
            return pending;
        }
    }
}
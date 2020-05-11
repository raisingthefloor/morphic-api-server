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
        public string ProcessorId { get; set; }
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
            ToEmail = user.GetEmail();
            EmailText = msg;
            Subject = subject;
            ProcessorId = "";
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

    public class EmailTemplates
    {
        // https://github.com/sendgrid/sendgrid-csharp/blob/master/USE_CASES.md#transactional-templates
        // TODO i18n? localization?
        private const string EmailVerificationMsgTemplate = 
            @"Dear {0},

To verify your email Address {1} please click the following link: {2}

Regards,
{3} ({4})";
        
        public static async Task NewVerificationEmail(Database db, EmailSettings settings, User user, string urlTemplate)
        {
            if (settings.Type == EmailSettings.EmailTypeDisabled)
            {
                Log.Logger.Warning("EmailSettings.Disable is set");
                return;
            }

            var oneTimeToken = new OneTimeToken(user.Id);
            
            // Create the email message
            var link = urlTemplate.Replace("{oneTimeToken}", oneTimeToken.GetUnhashedToken());
            var msg = string.Format(EmailVerificationMsgTemplate,
                user.FullnameOrEmail(),
                user.GetEmail(),
                link,
                settings.EmailFromFullname, settings.EmailFromAddress);
            var pending = new PendingEmail(user, settings.EmailFromAddress, settings.EmailFromFullname, 
                "Email Verification", msg, 
                PendingEmail.EmailTypeEnum.EmailValidation);
            await db.Save(oneTimeToken);
            await db.Save(pending);
        }
    }
}
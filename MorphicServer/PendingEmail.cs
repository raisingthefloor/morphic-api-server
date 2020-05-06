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
using System.Threading.Tasks;

namespace MorphicServer
{
    /// <summary>
    /// Class for Pending Emails.
    ///
    /// TODO Implement some background task to actually send these.
    /// </summary>
    public class PendingEmail : Record
    {
        public string UserId { get; set; }
        public string ToEmail { get; set; }
        public string FromEmail { get; set; }
        public string EmailText { get; set; }

        public PendingEmail(string userId, string to, string from, string text)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            ToEmail = EncryptedField.FromPlainText(to).ToCombinedString();
            FromEmail = from; // it's us. No need to encrypt it.
            EmailText = EncryptedField.FromPlainText(text).ToCombinedString();
        }
    }

    public class EmailTemplates
    {
        // TODO i18n? localization?
        private const string EmailVerificationMsgTemplate = 
            @"Dear {0},

To verify your email Address {1} please click the following link: {2}.

Regards,

--
{3}";

        public static string GreetingName(string? firstName, string? lastName, string email)
        {
            string dearUser = "";

            if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
            {
                if (!string.IsNullOrEmpty(firstName)) dearUser = firstName!;
                if (!string.IsNullOrEmpty(lastName))
                {
                    if (dearUser == "")
                    {
                        dearUser = lastName;
                    }
                    else
                    {
                        dearUser += " " + lastName;
                    }
                }
            }
            else
            {
                dearUser = email;
            }

            return dearUser;
        }
        
        public static async Task NewVerificationEmail(Database db, User user, string urlTemplate)
        {
            var encrEmail = EncryptedField.FromCombinedString(user.EmailEncrypted!);

            bool isPrimary;
            var email = encrEmail.Decrypt(out isPrimary);
            
            var oneTimeToken = new OneTimeToken(user.Id);
            var link = urlTemplate.Replace("{oneTimeToken}", oneTimeToken.Token);
            var from = "support@morphic.world";
            var msg = string.Format(EmailVerificationMsgTemplate,
                GreetingName(user.FirstName, user.LastName, email),
                email,
                link,
                from);
            var pending = new PendingEmail(user.Id, email, from, msg);
            await db.Save(oneTimeToken);
            await db.Save(pending);
        }
    }
}
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

namespace Morphic.Server.Auth
{

    using Db;
    using Email;
    using Users;

    public class EmailNotVerifiedPasswordResetEmail : EmailJob
    {
        public EmailNotVerifiedPasswordResetEmail(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailVerificationEmail> logger, Database db) : base(morphicSettings, settings, logger, db)
        {
            EmailType = EmailConstants.EmailTypes.PasswordResetEmailNotValidated;
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
            await SendOneEmail(EmailType, Attributes);
        }
    }

}
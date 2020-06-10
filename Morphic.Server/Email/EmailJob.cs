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

namespace Morphic.Server.Email
{

    using Db;
    using Background;
    using Users;

    /// <summary>
    /// Class for Pending Emails.
    /// </summary>

    public abstract class EmailJob: BackgroundJob
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
        
        protected EmailJob(MorphicSettings morphicSettings, EmailSettings settings, ILogger<EmailJob> logger, Database db): base(morphicSettings)
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

    class EmailJobException : MorphicServerException
    {
        public EmailJobException(string error) : base(error)
        {
        }
    }
}
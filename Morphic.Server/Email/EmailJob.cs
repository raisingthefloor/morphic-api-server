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

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Morphic.Server.Email
{

    using Db;
    using Background;
    using Users;

    /// <summary>
    /// Class defining an email-background job. Each type of email subclasses this in its own way,
    /// because not ever email takes the same arguments.
    ///
    /// In essence, the email-type implements some class to take simple serializable arguments (no complex
    /// objects; see best-practices for Hangfire) it can use to build the Attributes needed for actual email
    /// sending. When done, it is expected to call <see cref="Morphic.Server.Email.EmailJob.SendOneEmail">
    /// to do the actual sending of the email.
    /// </summary>

    public abstract class EmailJob: BackgroundJob
    {
        // https://github.com/sendgrid/sendgrid-csharp/blob/master/USE_CASES.md#transactional-templates
        // TODO i18n? localization?

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

        protected EmailConstants.EmailTypes EmailType = EmailConstants.EmailTypes.None;

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
            Attributes.Add("EmailType", EmailType.ToString());
            Attributes.Add("ToUserName", user.FullnameOrEmail());
            Attributes.Add("ToEmail", user.Email.PlainText!);
            Attributes.Add("FromUserName", EmailSettings.EmailFromFullname);
            Attributes.Add("FromEmail", EmailSettings.EmailFromAddress);
            Attributes.Add("ClientIp", clientIp ?? UnknownClientIp);
            Attributes.Add("Link", link ?? "");
        }

        private const string EmailSendingMetricHistogramName = "email_send_duration";

        private static readonly Histogram EmailSendingHistogram = Metrics.CreateHistogram(
            EmailSendingMetricHistogramName,
            "Time it takes to send an email",
            new[] {"type", "destination", "success"});

        public async Task SendOneEmail(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            if (EmailSettings.Type == EmailSettings.EmailTypeDisabled) {
                throw new SendEmailException("Email sending disabled");
            }

            logger.LogDebug("SendOneEmail sending email {EmailType} {ClientIp}",
                emailAttributes["EmailType"], emailAttributes["ClientIp"]);
            var stopWatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                var worker = SendEmailWorkerFactory.Get(EmailSettings, logger);
                if (worker == null)
                {
                    throw new SendEmailException("Unknown email type " + EmailSettings.Type);
                }
                success = true;
                var id = await worker.SendTemplate(emailType, emailAttributes);
                logger.LogInformation("SendOneEmail: Send success. {EmailType} {ClientIp} {MessageId}",
                    emailAttributes["EmailType"], emailAttributes["ClientIp"], id);
            }
            finally
            {
                stopWatch.Stop();
                EmailSendingHistogram.Labels(emailAttributes["EmailType"], EmailSettings.Type, success.ToString())
                    .Observe(stopWatch.Elapsed.TotalSeconds);
            }


            if (!success)
            {
                throw new UnableToSendEmailException();
            }

        }

        private class SendEmailException : MorphicServerException
        {
            public SendEmailException()
            {
            }
            public SendEmailException(string error) : base(error)
            {
            }
        }

        private class UnableToSendEmailException : SendEmailException
        {
        }
        
        protected class EmailJobException : MorphicServerException
        {
            public EmailJobException(string error) : base(error)
            {
            }
        }
    }
}
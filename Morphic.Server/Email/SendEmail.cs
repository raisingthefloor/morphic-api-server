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
    /// <summary>
    /// </summary>
    public class SendEmail
    {
        private readonly ILogger logger;
        private readonly EmailSettings emailSettings;

        public SendEmail(EmailSettings emailSettings, ILogger logger)
        {
            this.logger = logger;
            this.emailSettings = emailSettings;
        }

        private const string EmailSendingMetricHistogramName = "email_send_duration";

        private static readonly Histogram EmailSendingHistogram = Metrics.CreateHistogram(
            EmailSendingMetricHistogramName,
            "Time it takes to send an email",
            new[] {"type", "destination", "success"});

        public async Task SendOneEmail(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeDisabled) {
                throw new SendEmailException("Email sending disabled");
            }

            logger.LogInformation("SendOneEmail sending email {EmailType} {ClientIp}",
                emailAttributes["EmailType"], emailAttributes["ClientIp"]);
            var stopWatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                SendEmailWorker worker;
                if (emailSettings.Type == EmailSettings.EmailTypeSendgrid)
                {
                    worker = new Sendgrid(emailSettings, logger);
                }
                else if (emailSettings.Type == EmailSettings.EmailTypeSendInBlue)
                {
                    worker = new SendInBlue(emailSettings, logger);
                }
                else if (emailSettings.Type == EmailSettings.EmailTypeLog)
                {
                    worker = new EmailLogger(emailSettings, logger);
                }
                else
                {
                    throw new SendEmailException("Unknown email type " + emailSettings.Type);
                }
                success = await worker.SendTemplate(emailType, emailAttributes);
            }
            finally
            {
                stopWatch.Stop();
                EmailSendingHistogram.Labels(emailAttributes["EmailType"], emailSettings.Type, success.ToString())
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
    }
    
    public abstract class SendEmailWorker
    {
        protected readonly ILogger logger;
        protected readonly EmailSettings emailSettings;

        public SendEmailWorker(EmailSettings emailSettings, ILogger logger)
        {
            this.emailSettings = emailSettings;
            this.logger = logger;
        }

        public abstract Task<bool> SendTemplate(EmailConstants.EmailTypes emailType,
            Dictionary<string, string> emailAttributes);

    }

    public class EmailLogger : SendEmailWorker
    {
        public EmailLogger(EmailSettings emailSettings, ILogger logger) : base(emailSettings, logger)
        {
        }

        public override async Task<bool> SendTemplate(EmailConstants.EmailTypes emailType, Dictionary<string, string> emailAttributes)
        {
            logger.LogWarning("Debug Email Logging {EmailType}, {Attributes}",
                emailType, string.Join(" ", emailAttributes));
            return true;
        }
    }
}
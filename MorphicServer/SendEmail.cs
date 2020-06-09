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
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MorphicServer
{
    public class SendGridSettings
    {
        /// <summary>
        /// The Sendgrid API key.
        /// 
        /// NOTE: Do not put this into any appsettings file. It's a secret and should be
        /// configured via environment variables
        ///
        /// For production: Put it in repo: deploy-morphiclite, path: environments/*/secrets/all.env
        /// depending on the environment
        ///
        /// For development and others, see launchSettings.json or the docker-compose.morphicserver.yml file.
        /// </summary>
        public string ApiKey { get; set; } = "";
    }
    
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

        public async Task SendOneEmail(string emailTemplateId, Dictionary<string, string> emailAttributes)
        {
            if (emailSettings.Type == EmailSettings.EmailTypeDisabled ||
                (emailSettings.Type == EmailSettings.EmailTypeSendgrid &&
                 (emailSettings.SendGridSettings == null || emailSettings.SendGridSettings.ApiKey == "")))
            {
                logger.LogError("Email sending disabled or misconfigured. Check EmailSettings.Type and " +
                                "SendGrid Api Key. Note this task can be retried manually from the Hangfire console");
                return;
            }

            logger.LogInformation("SendOneEmail sending email {EmailType} {ClientIp}",
                emailAttributes["EmailType"], emailAttributes["ClientIp"]);
            bool sent = false;
            if (emailSettings.Type == EmailSettings.EmailTypeSendgrid)
            {
                sent = await SendViaSendGridDynamicTemplate(emailTemplateId, emailAttributes);
            }
            else if (emailSettings.Type == EmailSettings.EmailTypeLog)
            {
                LogEmailDynamicTemplate(emailTemplateId, emailAttributes);
                sent = true;
            }

            if (!sent)
            {
                throw new UnableToSendEmailException();
            }

        }

        private class SendEmailException : MorphicServerException
        {
        }

        private class UnableToSendEmailException : SendEmailException
        {

        }

        private async Task<bool> SendViaSendGridDynamicTemplate(string emailTemplateId,
            Dictionary<string, string> emailAttributes)
        {
            var stopWatch = Stopwatch.StartNew();
            bool success = false;
            try
            {
                var from = new EmailAddress(emailAttributes["FromEmail"], emailAttributes["FromUserName"]);
                var to = new EmailAddress(emailAttributes["ToEmail"], emailAttributes["ToUserName"]);
                var msg = MailHelper.CreateSingleTemplateEmail(from, to, emailTemplateId, emailAttributes);
                success = await SendViaSendGrid(msg);
                return success;
            }
            finally
            {
                stopWatch.Stop();
                EmailSendingHistogram.Labels(emailAttributes["EmailType"], "sendgrid", success.ToString())
                    .Observe(stopWatch.Elapsed.TotalSeconds);
            }
        }

        private async Task<bool> SendViaSendGrid(SendGridMessage msg)
        {
            var client = new SendGridClient(emailSettings.SendGridSettings.ApiKey);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.Ambiguous)
            {
                logger.LogError("Email send failed: {StatusCode} {Headers} {Body}", response.StatusCode,
                    response.Headers, response.Body.ReadAsStringAsync().Result);
                return false;
            }
            else
            {
                logger.LogDebug("Email send succeeded: {StatusCode} {Headers} {Body}", response.StatusCode,
                    response.Headers, response.Body.ReadAsStringAsync().Result);
                return true;
            }
        }
        
        private void LogEmailDynamicTemplate(string emailTemplateId, Dictionary<string, string> emailAttributes)
        {
            logger.LogWarning("Debug Email Logging {EmailTemplateId}, {Attributes}",
                emailTemplateId, string.Join(" ", emailAttributes));
        }

    }
}
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
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Auth
{
    using Http;
    using Users;
    
    [Path("/v1/user/verify_email_request")]
    public class VerifyEmailRequestUnauthEndpoint : Endpoint
    {
        private IBackgroundJobClient jobClient;
        private IRecaptcha recaptcha;

        public VerifyEmailRequestUnauthEndpoint(
            IHttpContextAccessor contextAccessor,
            ILogger<VerifyEmailRequestUnauthEndpoint> logger,
            IRecaptcha recaptcha,
            IBackgroundJobClient jobClient)
            : base(contextAccessor, logger)
        {
            AddAllowedOrigin(settings.FrontEndServerUri);
            this.recaptcha = recaptcha;
            this.jobClient = jobClient;
        }
        
        /// <summary>Resend the welcome email.</summary>
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<VerifyEmailRequestRequest>();
            if (request.GRecaptchaResponse == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadVerificationRequestResponse.MissingRequired(new List<string> { "g_captcha_response" }));
            }
            if (!await recaptcha.ReCaptchaPassed("verifyemailrequest", request.GRecaptchaResponse))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadVerificationRequestResponse.BadReCaptcha);
            }
            if (request.Email == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadVerificationRequestResponse.MissingRequired(new List<string> { "email" }));
            }

            if (!User.IsValidEmail(request.Email))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadVerificationRequestResponse.BadEmailAddress);
            }
            var db = Context.GetDatabase();
            var user = await db.UserForEmail(request.Email, ActiveSession);
            if (user == null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadVerificationRequestResponse.UserNotFound);
            }

            var hash = user.Email.Hash!.ToCombinedString();
            logger.LogInformation("Verify Email requested for userId {userId} {EmailHash}",
                user.Id, hash);
            jobClient.Enqueue<EmailVerificationEmail>(x => x.SendEmail(
                user.Id,
                Request.ClientIp()
            ));
        }
        
        /// <summary>
        /// Model the Verify-Email-Request Request
        /// </summary>
        public class VerifyEmailRequestRequest
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = null!;
            
            [JsonPropertyName("g_recaptcha_response")]
            public string GRecaptchaResponse { get; set; } = null!;
        }

        public class BadVerificationRequestResponse : BadRequestResponse
        {
            public static readonly BadVerificationRequestResponse UserNotFound = new BadVerificationRequestResponse("invalid_user");
            public static readonly BadVerificationRequestResponse BadReCaptcha = new BadVerificationRequestResponse("bad_recaptcha");
            public static readonly BadVerificationRequestResponse BadEmailAddress = new BadVerificationRequestResponse("bad_email_address");

            public static BadVerificationRequestResponse MissingRequired(List<string> missing)
            {
                return new BadVerificationRequestResponse(
                    "missing_required",
                    new Dictionary<string, object>
                    {
                        {"required", missing}
                    });
            }

            public BadVerificationRequestResponse(string error, Dictionary<string, object> details) : base(error, details)
            {
            }

            public BadVerificationRequestResponse(string error) : base(error)
            {
            }
        }
    }
}
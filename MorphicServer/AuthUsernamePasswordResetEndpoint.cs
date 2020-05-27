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
using MorphicServer.Attributes;
using Serilog.Context;

namespace MorphicServer
{
    /// <summary>
    /// API To reset a user's username-auth credential password. hence: AuthUsernamePasswordResetEndpoint
    /// (trust me, it gets worse in the other endpoint).
    ///
    /// This will reset the password to the given password, if the one-time-token is valid.
    /// </summary>
    [Path("/v1/auth/username/password_reset/{oneTimeToken}")]
    public class AuthUsernamePasswordResetEndpoint : Endpoint
    {
        private IRecaptcha recaptcha;
        
        public AuthUsernamePasswordResetEndpoint(IHttpContextAccessor contextAccessor, ILogger<AuthUsernameEndpoint> logger, IRecaptcha recaptcha): base(contextAccessor, logger)
        {
            this.recaptcha = recaptcha;
        }

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter] public string oneTimeToken = "";

        /// <summary>The UsernameCredential data populated by <code>LoadResource()</code></summary>
        private UsernameCredential usernameCredentials = null!;

        /// <summary>The limited-use token data populated by <code>LoadResource()</code></summary>
        public OneTimeToken OneTimeToken = null!;

        public override async Task LoadResource()
        {
            var hashedToken = OneTimeToken.TokenHashedWithDefault(oneTimeToken);
            try
            {
                OneTimeToken = await Load<OneTimeToken>(hashedToken);
                if (OneTimeToken == null || !OneTimeToken.IsValid())
                {
                    throw new HttpError(HttpStatusCode.NotFound, BadPasswordResetResponse.InvalidToken);
                }
            }
            catch (HttpError httpError)
            {
                throw new HttpError(httpError.Status, BadPasswordResetResponse.InvalidToken);
            }

            try
            {
                usernameCredentials = await Load<UsernameCredential>(u => u.UserId == OneTimeToken.UserId);
            }
            catch (HttpError httpError)
            {
                throw new HttpError(httpError.Status, BadPasswordResetResponse.UserNotFound);
            }
        }

        /// <summary>Reset the password</summary>
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<PasswordResetRequest>();
            if (request.GRecaptchaResponse == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordResetResponse.MissingRequired(new List<string> { "g_captcha_response" }));
            }
            if (!await recaptcha.ReCaptchaPassed(request.GRecaptchaResponse))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordResetResponse.BadReCaptcha);
            }
            
            if (request.NewPassword == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordResetResponse.MissingRequired(new List<string> {"new_password"}));
            }
            usernameCredentials.CheckAndSetPassword(request.NewPassword);
            await Save(usernameCredentials);
            await OneTimeToken.Invalidate(Context.GetDatabase());
            if (request.DeleteExistingTokens)
            {
                await Delete<AuthToken>(token => token.UserId == usernameCredentials.UserId);
            }
        }
        
        public class PasswordResetRequest
        {
            [JsonPropertyName("new_password")]
            public string NewPassword { get; set; } = null!;

            [JsonPropertyName("delete_existing_tokens")]
            public bool DeleteExistingTokens { get; set; } = false;

            // TODO Not sure if we can use underscores here. Depends on whether the caller reformats the data.
            [JsonPropertyName("g_recaptcha_response")]
            public string GRecaptchaResponse { get; set; } = null!;
        }

        public class BadPasswordResetResponse : BadRequestResponse
        {
            public static readonly BadPasswordResetResponse InvalidToken = new BadPasswordResetResponse("invalid_token");
            public static readonly BadPasswordResetResponse UserNotFound = new BadPasswordResetResponse("invalid_user");
            public static readonly BadPasswordResetResponse BadReCaptcha = new BadPasswordResetResponse("bad_recaptcha");

            public static BadPasswordResetResponse MissingRequired(List<string> missing)
            {
                return new BadPasswordResetResponse(
                    "missing_required",
                    new Dictionary<string, object>
                    {
                        {"required", missing}
                    });
            }

            public BadPasswordResetResponse(string error) : base(error)
            {
            }
        
            public BadPasswordResetResponse(string error, Dictionary<string, object> details) : base(error, details)
            {
            }
        }

    }

    /// <summary>
    /// The API to request a password-reset link for username auth. Therefore AuthUsernamePasswordResetRequestEndpoint.
    ///
    /// This will send an email to the email in the request json, whether that email exists or not.
    ///
    /// TODO Rate limit the emails we send to a given email, especially this stuff.
    /// </summary>
    [Path("/v1/auth/username/password_reset/request")]
    public class AuthUsernamePasswordResetRequestEndpoint : Endpoint
    {
        private IRecaptcha recaptcha;
        private IBackgroundJobClient jobClient;
        
        public AuthUsernamePasswordResetRequestEndpoint(
            IHttpContextAccessor contextAccessor, 
            ILogger<AuthUsernameEndpoint> logger,
            IRecaptcha recaptcha,
            IBackgroundJobClient jobClient): base(contextAccessor, logger)
        {
            this.recaptcha = recaptcha;
            this.jobClient = jobClient;
        }

        /// <summary>
        /// Process a request to send a password reset link. Requires a captcha result to be sent.
        /// </summary>
        /// <returns></returns>
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<PasswordResetRequestRequest>();
            if (request.GRecaptchaResponse == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordRequestResponse.MissingRequired(new List<string> { "g_captcha_response" }));
            }
            if (!await recaptcha.ReCaptchaPassed(request.GRecaptchaResponse))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordRequestResponse.BadReCaptcha);
            }
            
            if (request.Email == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordRequestResponse.MissingRequired(new List<string> { "email" }));
            }

            if (!User.IsValidEmail(request.Email))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadPasswordRequestResponse.BadEmailAddress);
            }
            var db = Context.GetDatabase();
            var hash = User.UserEmailHashCombined(request.Email);
            using (LogContext.PushProperty("EmailHash", hash))
            {
                var user = await db.Get<User>(a => a.EmailHash == hash, ActiveSession);
                if (user != null)
                {
                    if (user.EmailVerified)
                    {
                        logger.LogInformation("Password reset requested for userId {userId}", user.Id);
                        try
                        {
                            var linkTemplate = Context.GetMorphicSettings().ResetServerUrlTemplate;
                            if (linkTemplate.Contains("{self}"))
                            {
                                try
                                {
                                    var serverUrl = GetServerUrl(Request.Headers, Context.GetMorphicSettings());
                                    linkTemplate = linkTemplate.Replace("{self}", serverUrl);
                                }
                                catch (NoServerUrlFoundException e)
                                {
                                    logger.LogError("No server URL could be found. {Exception}", e.ToString());
                                    throw new HttpError(HttpStatusCode.InternalServerError);
                                }
                                catch (NotValidPathException e)
                                {
                                    logger.LogError("No valid path for class could be found. {Exception}", e.ToString());
                                    throw new HttpError(HttpStatusCode.InternalServerError);
                                }
                            }

                            jobClient.Enqueue<PasswordResetEmail>(x => x.QueueEmail(user.Id,
                                linkTemplate,
                                Request.ClientIp()));
                        }
                        catch (NoServerUrlFoundException e)
                        {
                            logger.LogError("Could not create the URL for the email-link. " +
                                            "For a quick fix, set MorphicSettings.ServerUrlPrefix {Exception}",
                                e.ToString());
                            throw new HttpError(HttpStatusCode.InternalServerError);
                        }
                    }
                    else
                    {
                        logger.LogInformation(
                            "Password reset requested for userId {userId}, but email not verified", user.Id);
                        jobClient.Enqueue<EmailNotVerifiedPasswordResetEmail>(x => x.QueueEmail(
                            request.Email,
                            Request.ClientIp()));
                    }
                }
                else
                {
                    logger.LogInformation("Password reset requested but no email matching");
                    jobClient.Enqueue<UnknownEmailPasswordResetEmail>(x => x.QueueEmail(
                        request.Email,
                        Request.ClientIp()));
                }
            }
        }
        
        /// <summary>
        /// Model the password-reset-request request (yea I know...)
        /// </summary>
        public class PasswordResetRequestRequest
        {
            [JsonPropertyName("email")]
            public string Email { get; set; } = null!;
            
            // TODO Not sure if we can use underscores here. Depends on whether the caller reformats the data.
            [JsonPropertyName("g_recaptcha_response")]
            public string GRecaptchaResponse { get; set; } = null!;
        }
        
        public class BadPasswordRequestResponse : BadRequestResponse
        {
            public static readonly BadPasswordRequestResponse BadEmailAddress = new BadPasswordRequestResponse("bad_email_address");
            public static readonly BadPasswordRequestResponse BadReCaptcha = new BadPasswordRequestResponse("bad_recaptcha");

            public static BadPasswordRequestResponse MissingRequired(List<string> missing)
            {
                return new BadPasswordRequestResponse(
                    "missing_required",
                    new Dictionary<string, object>
                    {
                        {"required", missing}
                    });
            }

            public BadPasswordRequestResponse(string error, Dictionary<string, object> details) : base(error, details)
            {
            }
            public BadPasswordRequestResponse(string error) : base(error)
            {
            }
        }

    }
}
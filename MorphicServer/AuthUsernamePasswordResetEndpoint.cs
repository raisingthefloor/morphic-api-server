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
    ///
    /// TODO: This is an API and not a web-page. We probably should have a web-page for this.
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

        [Method]
        public async Task Get()
        {
            // We're posting back to the same URL with the same OneTimeToken. We COULD/SHOULD create a new one, perhaps.
            // But this is just a short-term hack for testers to be able to reset passwords.
            var link = $"{Request.Scheme}://{Request.Host}{Request.Path}";

            var script = "<script src=\"https://www.google.com/recaptcha/api.js\"></script>";
            var script2 = "<script>function onSubmit(token) { document.getElementById(\"PasswordResetForm\").submit();}</script>";
            var recaptchaButton = $"<button class=\"g-recaptcha\" data-sitekey=\"{recaptcha.Key}\" data-callback='onSubmit' data-action='submit'>Submit</button>";

            var passwordInput = "<p><label for=\"new_password\">Password:</label><input type=\"password\" name=\"new_password\"><br>";
            var deleteExistingTokens = "<p><label for=\"delete_existing_tokens\">Delete all Auth Tokens:</label><input type=\"checkbox\" name=\"delete_existing_tokens\" value=\"true\"><br>";
            var form = $"<form action=\"{link}\" method=\"POST\" id=\"PasswordResetForm\" name=\"PasswordResetForm\">{passwordInput}{deleteExistingTokens}{recaptchaButton}</form>";
            var head = $"<head>{script}{script2}<title>PasswordResetForm</title></head>";
            var body = $"<body>{form}</body>";
            await Response.WriteHtml($"<html>{head}{body}</html>");
        }
        
        /// <summary>Reset the password</summary>
        [Method]
        public async Task Post()
        {
            PasswordResetRequest request;
            if (Request.ContentType.Contains("application/json"))
            {
                request = await Request.ReadJson<PasswordResetRequest>();
            } 
            else if (Request.ContentType.Contains("application/x-www-form-urlencoded"))
            {
                request = await FromForm(Request);
            }
            else
            {
                throw new HttpError(HttpStatusCode.BadRequest);
            }
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

            // TODO Need to respond with a nicer webpage than this
            await Respond(new SuccessResponse("password_was_reset"));
        }

        private async Task<PasswordResetRequest> FromForm(HttpRequest request)
        {
            var result = await request.GetHtmlBodyStringAsync();
            var resetRequest = new PasswordResetRequest();
            var elements = result.Split("&");
            foreach (var el in elements)
            {
                var parts = el.Split("=");
                if (parts[0] == "new_password")
                {
                    resetRequest.NewPassword = parts[1];
                }
                else if (parts[0] == "delete_existing_tokens")
                {
                    if (parts[1] == "true")
                    {
                        resetRequest.DeleteExistingTokens = true;
                    }
                }
                else if (parts[0] == "g-recaptcha-response")
                {
                    resetRequest.GRecaptchaResponse = parts[1];
                }
                else
                {
                    throw new HttpError(HttpStatusCode.BadRequest);
                }
            }

            return resetRequest;
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

        public class SuccessResponse
        {
            [JsonPropertyName("message")]
            public string Status { get; }

            public SuccessResponse(string message)
            {
                Status = message;
            }
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

        [Method]
        public async Task Get()
        {
            // short-term hack for testers to be able to reset passwords.
            var link = $"{Request.Scheme}://{Request.Host}{Request.Path}";

            var script = "<script src=\"https://www.google.com/recaptcha/api.js\"></script>";
            var script2 = "<script>function onSubmit(token) { document.getElementById(\"PasswordResetRequestForm\").submit();}</script>";
            var recaptchaButton = $"<button class=\"g-recaptcha\" data-sitekey=\"{recaptcha.Key}\" data-callback='onSubmit' data-action='submit'>Submit</button>";
            
            var emailInput = "<p><label for=\"email\">Email:</label><input type=\"text\" name=\"email\"><br>";
            var form = $"<form action=\"{link}\" method=\"POST\" id=\"PasswordResetRequestForm\" name=\"PasswordResetRequestForm\">{emailInput}{recaptchaButton}</form>";
            var head = $"<head>{script}{script2}<title>PasswordResetRequestForm</title></head>";
            var body = $"<body>{form}</body>";
            await Response.WriteHtml($"<html>{head}{body}</html>");
        }

        /// <summary>
        /// TODO: Need to rate-limit this and/or use re-captcha
        /// </summary>
        /// <returns></returns>
        [Method]
        public async Task Post()
        {
            PasswordResetRequestRequest request;
            if (Request.ContentType.Contains("application/json"))
            {
                request = await Request.ReadJson<PasswordResetRequestRequest>();
            } 
            else if (Request.ContentType.Contains("application/x-www-form-urlencoded"))
            {
                request = await FromForm(Request);
            }
            else
            {
                throw new HttpError(HttpStatusCode.BadRequest);
            }

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
                            jobClient.Enqueue<PasswordResetEmail>(x => x.QueueEmail(user.Id,
                                GetControllerPathUrl<AuthUsernamePasswordResetEndpoint>(Request.Headers,
                                    Context.GetMorphicSettings()),
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
            // TODO Need to respond with a nicer webpage than this
            await Respond(new SuccessResponse("password_reset_sent"));
        }

        public class SuccessResponse
        {
            [JsonPropertyName("message")]
            public string Status { get; }

            public SuccessResponse(string message)
            {
                Status = message;
            }
        }

        private async Task<PasswordResetRequestRequest> FromForm(HttpRequest request)
        {
            var result = await request.GetHtmlBodyStringAsync();
            var resetRequest = new PasswordResetRequestRequest();
            var elements = result.Split("&");
            foreach (var el in elements)
            {
                var parts = el.Split("=");
                if (parts[0] == "email")
                {
                    resetRequest.Email = parts[1];
                }
                else if (parts[0] == "g-recaptcha-response")
                {
                    resetRequest.GRecaptchaResponse = parts[1];
                }
                else
                {
                    throw new HttpError(HttpStatusCode.BadRequest);
                }
            }

            return resetRequest;
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
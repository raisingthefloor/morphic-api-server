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

using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Morphic.Server.Auth
{

    using Http;
    using Users;

    [Path("/v1/users/{userId}/verify_email/{oneTimeToken}")]
    public class VerifyEmailEndpoint : Endpoint
    {
        public VerifyEmailEndpoint(IHttpContextAccessor contextAccessor, ILogger<VerifyEmailEndpoint> logger): base(contextAccessor, logger)
        {
            AddAllowedOrigin(settings.FrontEndServerUri);
        }

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string oneTimeToken = "";

        /// <summary>
        /// The userid to use, populated from the request URL. Unused, since we get it from the token,
        /// but it makes for a more coherent API.
        /// </summary>
        [Parameter]
        public string UserId = "";

        /// <summary>The user data populated by <code>LoadResource()</code></summary>
        private User user = null!;
        /// <summary>The limited-use token data populated by <code>LoadResource()</code></summary>
        private OneTimeToken OneTimeToken = null!;

        public override async Task LoadResource()
        {
            try
            {
                var token = await Context.GetDatabase().TokenForToken(oneTimeToken, ActiveSession) ?? null;
                if (token == null || !token.IsValid())
                {
                    throw new HttpError(HttpStatusCode.NotFound, BadVerificationResponse.InvalidToken);
                }
                OneTimeToken = token;
            }
            catch (HttpError httpError)
            {
                throw new HttpError(httpError.Status, BadVerificationResponse.InvalidToken);
            }
            
            try
            {
                user = await Load<User>(OneTimeToken.UserId) ?? throw new HttpError(HttpStatusCode.BadRequest,
                           BadVerificationResponse.UserNotFound);
            }
            catch (HttpError httpError)
            {
                throw new HttpError(httpError.Status, BadVerificationResponse.UserNotFound);
            }
        }
        
        /// <summary>Mark the email verified</summary>
        [Method]
        public async Task Get()
        {
            // backwards compatibility for a release or two.
            await Post();
        }

        [Method]
        public async Task Post()
        {
            user.EmailVerified = true;
            await Save(user);
            await OneTimeToken.Invalidate(Context.GetDatabase());
            await Respond(new SuccessResponse("email_verified"));
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

        public class BadVerificationResponse : BadRequestResponse
        {
            public static readonly BadVerificationResponse InvalidToken = new BadVerificationResponse("invalid_token");
            public static readonly BadVerificationResponse UserNotFound = new BadVerificationResponse("invalid_user");

            public BadVerificationResponse(string error) : base(error)
            {
            }

        }
    }
}
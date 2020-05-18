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
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MorphicServer.Attributes;

namespace MorphicServer
{
    [Path("/v1/users/{userId}/verify_email/{oneTimeToken}")]
    public class ValidateEmailEndpoint : Endpoint
    {
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
            var hashedToken = OneTimeToken.TokenHashedWithDefault(oneTimeToken);
            try
            {
                OneTimeToken = await Load<OneTimeToken>(hashedToken);
                if (OneTimeToken == null || !OneTimeToken.IsValid())
                {
                    throw new HttpError(HttpStatusCode.NotFound, BadVerificationResponse.InvalidToken);
                }
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
        
        /// <summary>Fetch the user</summary>
        [Method]
        public async Task Get()
        {
            user.EmailVerified = true;
            await Save(user);
            await OneTimeToken.Invalidate(Context.GetDatabase());
            // TODO Need to respond with a nicer webpage than ""
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

        public class ValidateEmailEndpointException : MorphicServerException
        {
            protected ValidateEmailEndpointException(string error) : base(error)
            {
            }

            protected ValidateEmailEndpointException()
            {
            }
        }
        
        public class NoServerUrlFoundException : ValidateEmailEndpointException
        {
            public NoServerUrlFoundException(string error) : base(error)
            {
            }

            public NoServerUrlFoundException()
            {
            }
        }
        
        public class NotValidPathException : ValidateEmailEndpointException
        {
            public NotValidPathException(string error) : base(error)
            {
            }
        }
        
        public static string GetEmailVerificationLinkTemplate(IHeaderDictionary requestHeaders, MorphicSettings morphicSettings)
        {
            string pathTemplate = typeof(ValidateEmailEndpoint).GetRoutePath() ?? throw new NotValidPathException("No path");
            if (!pathTemplate.StartsWith("/"))
            {
                throw new NotValidPathException(pathTemplate);
            }
            string serverUrl = morphicSettings.ServerUrlPrefix ?? "";
            if (serverUrl != "")
            {
                // validate it really is a URL
                try
                {
                    var url = new Uri(serverUrl);
                    if (url.Host == "" || url.Scheme == "")
                    {
                        throw new NoServerUrlFoundException(serverUrl);
                    }
                }
                catch (UriFormatException e)
                {
                    throw new NoServerUrlFoundException($"{serverUrl}: {e.Message}");
                }
                char[] charsToTrim = {'/'}; // in case a human added the trailing slash in the settings.
                serverUrl = serverUrl.TrimEnd(charsToTrim);
            }
            else
            {
                // try to assemble it from X-Forwarded-For- headers.
                var host = requestHeaders["x-forwarded-host"].ToString();
                var scheme = requestHeaders["x-forwarded-proto"].ToString();
                if (host == "" || scheme == "")
                {
                    throw new NoServerUrlFoundException();
                }

                serverUrl = $"{scheme}://{host}";
                var port = requestHeaders["x-forwarded-port"].ToString();
                if (port != "" && ((scheme == "http" && port != "80") || (scheme == "https" && port != "443")))
                {
                    serverUrl += $":{port}";
                }
            }

            return $"{serverUrl}{pathTemplate}";
        }
    }
}
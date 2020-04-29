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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;
using System.Text.Json.Serialization;
using Serilog;

namespace MorphicServer
{

    public class RegisterEndpoint<CredentialType> : Endpoint where CredentialType: Credential
    {

        protected async Task Register(CredentialType credential, RegisterRequest request)
        {
            var prefs = new Preferences();
            prefs.Id = Guid.NewGuid().ToString();
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.FirstName = request.firstName;
            user.LastName = request.lastName;
            user.PreferencesId = prefs.Id;
            user.TouchLastAuth(); // technically, this is an auth, since we return a token!
            credential.UserId = user.Id;
            prefs.UserId = user.Id;
            var token = new AuthToken(user);
            await Save(prefs);
            await Save(user);
            await Save(credential);
            await Save(token);
            var response = new AuthResponse();
            response.token = token.Id;
            response.user = user;
            await Respond(response);

        }

        protected class RegisterRequest
        {
            [JsonPropertyName("first_name")]
            public string? firstName { get; set; }
            [JsonPropertyName("last_name")]
            public string? lastName { get; set; }
        }
    }

    /// <summary>Create a new user with a username</summary>
    [Path("/v1/register/username")]
    public class RegisterUsernameEndpoint: RegisterEndpoint<UsernameCredential>
    {
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<RegisterUsernameRequest>();
            if (String.IsNullOrWhiteSpace(request.username) || String.IsNullOrWhiteSpace(request.password)){
                 
                Log.Logger.Information("MISSING_USERNAME_PASSWORD");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.MissingRequired);
            }

            checkPassword(request.password);

            var existing = await Context.GetDatabase().Get<UsernameCredential>(request.username, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.ExistingUsername);
            }

            var cred = new UsernameCredential();
            cred.Id = request.username;
            cred.SavePassword(request.password);
            await Register(cred, request);
        }

        static private readonly int MinPasswordLength = 6;
        public static readonly ReadOnlyCollection<string> BadPasswords = new ReadOnlyCollection<string>(
            new string[] {
                "password",
                "testing"
            }
        );

        private void checkPassword(String password)
        {
            if (password.Length < MinPasswordLength)
            {
                Log.Logger.Information("SHORT_PASSWORD({username})");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.ShortPassword);
            }

            if (BadPasswords.Contains(password))
            {
                Log.Logger.Information("KNOWN_BAD_PASSWORD({username})");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.BadPassword);
            }
        }
        class RegisterUsernameRequest : RegisterRequest
        {
            [JsonPropertyName("username")]
            public string username { get; set; } = "";
            [JsonPropertyName("password")]
            public string password { get; set; } = "";
        }

        class BadRequestResponseUser : BadRequestResponse
        {
            public static readonly BadRequestResponse ExistingUsername = new BadRequestResponseUser("existing_username");
            public static readonly BadRequestResponse MissingRequired = new BadRequestResponseUser("missing_required");
            public static readonly BadRequestResponse ShortPassword = new BadRequestResponseUser(
                "short_password",
                new Dictionary<string, object>
                {
                    {"minimum_length", MinPasswordLength}
                });
            public static readonly BadRequestResponse BadPassword = new BadRequestResponseUser("bad_password");

            public BadRequestResponseUser(string error, Dictionary<string, object> details) : base(error, details)
            {
            }

            private BadRequestResponseUser(string error) : base(error)
            {
            }
        }
    }

    /// <summary>Create a new user with a username</summary>
    // Disabling until we have a legitimate use case.  Not removing because we expect
    // to re-enable at some point down the line for something like a USB stick login.
    // [Path("/v1/register/key")]
    public class RegisterKeyEndpoint: RegisterEndpoint<KeyCredential>
    {
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<RegisterKeyRequest>();
            if (String.IsNullOrWhiteSpace(request.key))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseKey.MissingRequired);
            }
            var existing = await Context.GetDatabase().Get<KeyCredential>(request.key, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseKey.ExistingKey);
            }
            var cred = new KeyCredential();
            cred.Id = request.key;
            await Register(cred, request);
        }

        class RegisterKeyRequest : RegisterRequest
        {
            [JsonPropertyName("key")]
            public string key { get; set; } = "";
        }

        class BadRequestResponseKey : BadRequestResponse
        {
            public static readonly BadRequestResponse ExistingKey = new BadRequestResponseKey("existing_key");
            public static readonly BadRequestResponse MissingRequired = new BadRequestResponseKey("missing_required");

            public BadRequestResponseKey(string error) : base(error)
            {
            }
        }
    }

    public class BadRequestResponse
    {
        [JsonPropertyName("error")] 
        public string Error { get; set; }

        [JsonPropertyName("details")]
        public Dictionary<string, object>? Details { get; set; }

        public BadRequestResponse(string error)
        {
            Error = error;
        }
        public BadRequestResponse(string error, Dictionary<string, object> details)
        {
            Error = error;
            Details = details;
        }
    }
}
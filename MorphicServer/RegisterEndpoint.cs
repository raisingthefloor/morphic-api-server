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
using System.Net.Mail;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace MorphicServer
{

    public class RegisterEndpoint<CredentialType> : Endpoint where CredentialType: Credential
    {

        protected async Task Register(CredentialType credential, User user)
        {
            var prefs = new Preferences();
            prefs.Id = Guid.NewGuid().ToString();
            user.PreferencesId = prefs.Id;
            user.TouchLastAuth(); // technically, this is an auth, since we return a token!
            prefs.UserId = user.Id;
            credential.UserId = user.Id;
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

        public class RegisterRequest
        {
            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; }
            [JsonPropertyName("last_name")]
            public string? LastName { get; set; }
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
            if (request.Username == "")
            {
                Log.Logger.Information("MISSING_USERNAME");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.MissingRequired);
            }

            CheckPassword(request.Password);
            await CheckEmail(request.Email);

            var existing = await Context.GetDatabase().Get<UsernameCredential>(request.Username, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.ExistingUsername);
            }
            
            var cred = new UsernameCredential();
            cred.Id = request.Username;
            cred.SetPassword(request.Password);
            
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.SetEmail(request.Email);
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            await Register(cred, user);
            BackgroundJob.Enqueue<NewVerificationEmail>(x => x.QueueEmail(
                user.Id,
                GetControllerPathUrl<ValidateEmailEndpoint>(Request.Headers, Context.GetMorphicSettings()),
                ClientIpFromRequest(Request)
            ));
        }

        private static bool IsValidEmail(string emailaddress)
        {
            try
            {
                // ReSharper disable once ObjectCreationAsStatement
                new MailAddress(emailaddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private const int MinPasswordLength = 6;

        private static readonly ReadOnlyCollection<string> BadPasswords = new ReadOnlyCollection<string>(
            new[] {
                "password",
                "testing"
            }
        );

        private static void CheckPassword(String password)
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

        private async Task CheckEmail(String email)
        {
            if (!IsValidEmail(email))
            {
                Log.Logger.Information("MALFORMED_EMAIL");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.MalformedEmail);
            }

            var hash = User.UserEmailHashCombined(email);
            var existingEmail = await Context.GetDatabase().Get<User>(a => a.EmailHash == hash, ActiveSession);
            if (existingEmail != null)
            {
                Log.Logger.Information("EMAIL_EXISTS({username})");
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.ExistingEmail);
            }
        }

        class RegisterUsernameRequest : RegisterRequest
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = null!;
            [JsonPropertyName("password")]
            public string Password { get; set; } = null!;

            [JsonPropertyName("email")] 
            public string Email { get; set; } = null!;
        }

        class BadRequestResponseUser : BadRequestResponse
        {
            public static readonly BadRequestResponse ExistingUsername = new BadRequestResponseUser("existing_username");
            public static readonly BadRequestResponse ExistingEmail = new BadRequestResponseUser("existing_email");
            public static readonly BadRequestResponse MalformedEmail = new BadRequestResponseUser("malformed_email");
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
            if (request.Key == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseKey.MissingRequired);
            }
            var existing = await Context.GetDatabase().Get<KeyCredential>(request.Key, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseKey.ExistingKey);
            }
            var cred = new KeyCredential();
            cred.Id = request.Key;
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.EmailVerified = false;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            await Register(cred, user);
        }

        class RegisterKeyRequest : RegisterRequest
        {
            [JsonPropertyName("key")]
            public string Key { get; set; } = null!;
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
}
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
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;
using Morphic.Security;

namespace Morphic.Server
{
    public class UsernameCredential : Credential
    {
        public SearchableHashedString Username { get; set; }
        public HashedData PasswordHash { get; set; }

        public UsernameCredential(string userId, string username, string password)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Username = new SearchableHashedString(username);
            PasswordHash = new HashedData(password);
        }

        public bool IsValidPassword(string password)
        {
            return PasswordHash.Equals(password);
        }
        
        private const int MinPasswordLength = 6;

        private static readonly ReadOnlyCollection<string> BadPasswords = new ReadOnlyCollection<string>(
            new[] {
                "password",
                "testing"
            }
        );

        public void CheckAndSetPassword(string password)
        {
            CheckPassword(password);
            PasswordHash = new HashedData(password);
        }

        public static void CheckPassword(String password)
        {   
            if (password.Length < MinPasswordLength)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.ShortPassword);
            }

            if (BadPasswords.Contains(password))
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponseUser.BadPassword);
            }
        }

        class BadRequestResponseUser : BadRequestResponse
        {
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

    public static class UsernameCredentialDatabase
    {
        private static readonly string counter_metric_name = "morphic_bad_user_auth";

        private static readonly Counter BadAuthCounter = Metrics.CreateCounter(counter_metric_name,
            "Bad User Authentications",
            new CounterConfiguration
            {
                LabelNames = new[] {"type"}
            });

        public static async Task<UsernameCredential?> UsernameCredentialForUsername(this Database db, string username, Database.Session? session = null)
        {
            var searchString = new SearchableEncryptedString(username).Hash!.ToCombinedString();
            return await db.Get<UsernameCredential>(uc => uc.Username == searchString, session);
        }

        public static async Task<User> UserForUsername(this Database db, string username, string password, Database.Session? session = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                db.logger.LogInformation("EmptyUsername");
                BadAuthCounter.Labels("EmptyUsername").Inc();
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.InvalidCredentials);
            }
            var credential = await db.UsernameCredentialForUsername(username, session);
            if (credential == null || credential.UserId == null)
            {
                db.logger.LogInformation("CredentialNotFound");
                BadAuthCounter.Labels("CredentialNotFound").Inc();
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.InvalidCredentials);
            }
            return await db.UserForUsernameCredential(credential, password, session);
        }
        
        public static async Task<User> UserForUsernameCredential(this Database db, UsernameCredential credential, string password, Database.Session? session = null)
        {
            DateTime? until = await db.UserLockedOut(credential.UserId!, session);
            if (until != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.Locked(until.GetValueOrDefault()));
            }

            if (!credential.IsValidPassword(password))
            {
                var lockedOut = await db.BadPasswordAuthAttempt(credential.UserId!);
                if (lockedOut)
                {
                    // no need to log anything. BadPasswordLockout.BadAuthAttempt() already did.
                    BadAuthCounter.Labels("UserLockedOut").Inc();
                }
                else
                {
                    db.logger.LogInformation("{UserId} InvalidPassword", credential.UserId);
                    BadAuthCounter.Labels("InvalidPassword").Inc();

                }
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.InvalidCredentials);
            }


            var user = await db.Get<User>(credential.UserId!);
            if (user == null)
            {
                // Not sure how this could happen: It means we have a credential for the user, but no user!
                // How did the credential get there if there's no user?
                db.logger.LogError("{UserId} UserNotFound from credential", credential.UserId);
                BadAuthCounter.Labels("UserNotFound").Inc();
                throw new HttpError(HttpStatusCode.InternalServerError);
            }
            return user;
        }
        
        class BadUserAuthResponse : BadRequestResponse
        {
            public static readonly BadRequestResponse InvalidCredentials = new BadUserAuthResponse("invalid_credentials");
            // Future use: public static readonly BadRequestResponse RateLimited = new BadUserAuthResponse("rate_limited");
                

            public BadUserAuthResponse(string error) : base(error)
            {
            }

            public static BadRequestResponse Locked(DateTime until)
            {
                var timeoutSeconds = until - DateTime.UtcNow;
                var response = new BadUserAuthResponse("locked");
                response.Details = new Dictionary<string, object>
                {
                    {"timeout", (int)timeoutSeconds.TotalSeconds}
                };
                return response;
            }
        }
    }
}
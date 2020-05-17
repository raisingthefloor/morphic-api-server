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
using System.Net;
using System.Threading.Tasks;
using Prometheus;
using Serilog;

namespace MorphicServer
{
    public class UsernameCredential : Credential
    {
        public int PasswordIterationCount;
        public string PasswordFunction = null!;
        public string PasswordSalt = null!;
        public string PasswordHash = null!;

        public void SetPassword(string password)
        {
            var hashedData = HashedData.FromString(password);
            PasswordFunction = hashedData.HashFunction;
            PasswordSalt = hashedData.Salt;
            PasswordIterationCount = hashedData.IterationCount;
            PasswordHash = hashedData.Hash;
        }

        public bool IsValidPassword(string password)
        {
            var hashedData = new HashedData(PasswordIterationCount, PasswordFunction, PasswordSalt, PasswordHash);
            return hashedData.Equals(password);
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

        public static async Task<User> UserForUsername(this Database db, string username, string password)
        {
            var credential = await db.Get<UsernameCredential>(username);
            if (credential == null || credential.UserId == null)
            {
                Log.Logger.Information("CredentialNotFound");
                BadAuthCounter.Labels("CredentialNotFound").Inc();
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.InvalidCredentials);
            }

            return await db.UserForUsernameCredential(credential, password);
        }
        
        public static async Task<User> UserForUsernameCredential(this Database db, UsernameCredential credential, string password)
        {
            DateTime? until = await BadPasswordLockout.UserLockedOut(db, credential.UserId!);
            if (until != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.Locked(until.GetValueOrDefault()));
            }

            if (!credential.IsValidPassword(password))
            {
                var lockedOut = await BadPasswordLockout.BadAuthAttempt(db, credential.UserId!);
                if (lockedOut)
                {
                    // no need to log anything. BadPasswordLockout.BadAuthAttempt() already did.
                    BadAuthCounter.Labels("UserLockedOut").Inc();
                }
                else
                {
                    Log.Logger.Information("{UserId} InvalidPassword", credential.UserId);
                    BadAuthCounter.Labels("InvalidPassword").Inc();

                }
                throw new HttpError(HttpStatusCode.BadRequest, BadUserAuthResponse.InvalidCredentials);
            }


            var user = await db.Get<User>(credential.UserId!);
            if (user == null)
            {
                // Not sure how this could happen: It means we have a credential for the user, but no user!
                // How did the credential get there if there's no user?
                Log.Logger.Error("{UserId} UserNotFound from credential", credential.UserId);
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
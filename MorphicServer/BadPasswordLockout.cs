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
using System.Globalization;
using System.Threading.Tasks;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{
    /// <summary>
    /// Class to handle bad password lockout. If a user uses a bad password MaxCount times
    /// over BadPasswordsInTimeSeconds then the logins will be blocked for LockOutForTimeSeconds
    /// and no further attempts allowed.
    ///
    /// Any login attempts, good or bad, will simply be ignored. Specifically bad-password logins will
    /// not extend the counter. So no matter what happens, after LockOutForTimeSeconds, the record is
    /// deleted and new logins are allowed (the counter reset, so to speak).
    ///
    /// TODO Should a successful login attempt reset (delete) this DB entry?
    /// </summary>
    public class BadPasswordLockout : Record
    {
        private const int MaxCount = 5;
        private const int BadPasswordsInTimeSeconds = 5 * 60;
        private const int LockOutForTimeSeconds = 15 * 60;
        
        public DateTime ExpiresAt { get; private set; }
        public int Count { get; private set; }
        
        private BadPasswordLockout(string userId)
        {
            Id = userId;
            Touch(BadPasswordsInTimeSeconds);
        }

        public static async Task<bool> BadAuthAttempt(Database db, string userId)
        {
            BadPasswordLockout badPasswordLockout = await db.Get<BadPasswordLockout>(userId) ?? new BadPasswordLockout(userId);
            badPasswordLockout.Count++;
            bool lockedOut = false;
            if (badPasswordLockout.ShouldBlockLogin())
            {
                // TODO If we do it this way, we keep pushing out the expiration with every new bad login. Maybe we don't want that?
                badPasswordLockout.Touch(LockOutForTimeSeconds);
                using (LogContext.PushProperty("UserUid", userId))
                using (LogContext.PushProperty("BadLoginCount", badPasswordLockout.Count))
                using (LogContext.PushProperty("Blocked Until", badPasswordLockout.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")))
                {
                    Log.Logger.Information("Blocking Logins");
                }

                lockedOut = true;
            }

            await db.Save(badPasswordLockout);
            return lockedOut;
        }

        public static async Task<DateTime?> UserLockedOut(Database db, string userId)
        {
            BadPasswordLockout badPasswordLockout = await db.Get<BadPasswordLockout>(userId) ?? new BadPasswordLockout(userId);
            if (badPasswordLockout.ShouldBlockLogin())
            {
                return badPasswordLockout.ExpiresAt;
            }

            return null;
        }

        public bool ShouldBlockLogin()
        {
            if (Count >= MaxCount)
            {
                return true;
            }

            return false;
        }
        public void Touch(int ttl)
        {
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, ttl);
        }
    }
}
    
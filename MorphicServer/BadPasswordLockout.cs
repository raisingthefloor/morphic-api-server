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
using System.Threading.Tasks;
using Serilog;

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
                badPasswordLockout.Touch(LockOutForTimeSeconds);
                Log.Logger.Information(
                    "Blocking Logins for {UserUid} after {BadLoginCount} attempts. Blocked until {BlockedUntil}",
                    userId, badPasswordLockout.Count, badPasswordLockout.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                lockedOut = true;
            }

            await db.Save(badPasswordLockout);
            return lockedOut;
        }

        public static async Task<DateTime?> UserLockedOut(Database db, string userId)
        {
            var badPasswordLockout = await db.Get<BadPasswordLockout>(userId);
            if (badPasswordLockout != null)
            {
                return badPasswordLockout.UserLockedOut();
            }

            // user is not locked out
            return null;
        }

        public DateTime? UserLockedOut()
        {
            if (ShouldBlockLogin())
            {
                return ExpiresAt;
            }

            // user is not locked out
            return null;
        }

        private bool ShouldBlockLogin()
        {
            if (Count >= MaxCount)
            {
                return true;
            }

            return false;
        }

        private void Touch(int ttl)
        {
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, ttl);
        }
    }
}
    
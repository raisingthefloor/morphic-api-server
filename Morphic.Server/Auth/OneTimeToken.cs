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
using Morphic.Security;

namespace Morphic.Server.Auth
{
    using Db;
    
    /// <summary>
    /// A One time Token class (and mongo collection).
    ///
    /// The Id is the hashed value of the token. We can't very well store the
    /// token unhashed (someone could find it in the DB and use it directly), so
    /// we hash it before storing. When we need to validate it, we again hash the
    /// received token, and look for it in the DB.
    /// </summary>
    public class OneTimeToken : Record
    {
        public string UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public SearchableHashedString Token { get; set; }
        private readonly string token;
        
        private const int DefaultExpiresSeconds = 30 * 24 * 60 * 60; // 2592000 seconds in 30 days
        
        public OneTimeToken(string userId, int expiresInSeconds = DefaultExpiresSeconds)
        {
            Id = Guid.NewGuid().ToString();
            token = NewToken();
            Token = new SearchableHashedString(token);
            UserId = userId;
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, expiresInSeconds);
        }

        // This method is necessary so that the email-sending functionality can get the original
        // unhashed token and put it in the email. This will only work when the OneTimeToken is first
        // initialized and still has the original value.
        public string GetUnhashedToken()
        {
            if (token == "")
            {
                // For the case that someone thinks they can load the data from the DB and
                // get the original un-hashed value. That won't work.
                throw new OneTimeTokenException("uninitialized unhashed token");
            }
            return token;
        }
        
        private static string NewToken()
        {
            // We don't use base64 here, since we will use the token in a URL and base64 includes the '/' character
            var data = EncryptedField.RandomBytes(32);
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
        
        public async Task Invalidate(Database db)
        {
            UsedAt = DateTime.UtcNow;
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(5); // keep the token for 5 days for debugging, perhaps.
            await db.Save(this);
        }
        
        // MongoDB says: "The background task that removes expired documents runs every 60 seconds.
        //    As a result, documents may remain in a collection during the period between the expiration
        //    of the document and the running of the background task."
        // https://docs.mongodb.com/manual/core/index-ttl/
        // The question is: Do we care about those 60 seconds?
        public bool IsValid()
        {
            return UsedAt == null && ExpiresAt > DateTime.UtcNow;
        }

        public class OneTimeTokenException : MorphicServerException
        {
            public OneTimeTokenException(string error) : base(error)
            {
            }
        }
    }
    
    public static class OneTimeTokenDatabase
    {
        public static async Task<OneTimeToken?> TokenForToken(this Database db, string email, Database.Session? session = null)
        {
            var hash = new SearchableHashedString(email).ToCombinedString();
            return await db.Get<OneTimeToken>(t => t.Token == hash, session);
        }
    }

}
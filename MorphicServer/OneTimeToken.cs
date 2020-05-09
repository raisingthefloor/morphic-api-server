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

namespace MorphicServer
{
    public class OneTimeToken : Record
    {
        public string UserId { get; set; }
        public string HashedToken { get; set; }
        public DateTime ExpiresAt { get; set; }

        private const int DefaultExpiresSeconds = 30 * 24 * 60 * 60; // 2592000 seconds in 30 days
        private const string DefaultTokenHash = "VpMZSDRh2nUiI/y2uARYbw==";
        
        public OneTimeToken(string userId, string token, int expiresInSeconds = DefaultExpiresSeconds)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            HashedToken = TokenHashedWithDefault(token);
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, expiresInSeconds);
        }

        public static string TokenHashedWithDefault(string token)
        {
            return HashedData.FromString(token, DefaultTokenHash).ToCombinedString();
        }
        public static string NewToken()
        {
            // We don't use base64 here, since we will use the token in a URL and base64 includes the '/' character
            var data = EncryptedField.RandomBytes(32);
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
        
        public async Task Invalidate(Database db)
        {
            await db.Delete(this);
        }
    }
}
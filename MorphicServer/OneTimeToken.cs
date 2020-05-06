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
        // TODO Should we encrypt or hash this? If we do, we can't retrieve it easily when we get it back.
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }

        private const int DefaultExpiresSeconds = 30 * 24 * 60 * 60; // 2592000 seconds in 30 days
        
        public OneTimeToken(string userId, int expiresInSeconds = DefaultExpiresSeconds)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Token = newToken();
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, expiresInSeconds);
        }

        private string newToken()
        {
            return EncryptedField.Random128BitsBase64();
        }
        
        public async Task Invalidate(Database db)
        {
            await db.Delete(this);
        }
    }
}
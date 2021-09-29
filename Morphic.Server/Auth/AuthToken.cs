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
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Morphic.Server.Auth
{

    using Db;
    using Users;

    public class AuthToken: Record
    {
        public string UserId;
        public DateTime ExpiresAt = DateTime.Now;

        public AuthToken(User user, int ttl = 4 * 60 * 60)
        {
            var provider = RandomNumberGenerator.Create();
            var data = new byte[64];
            provider.GetBytes(data);
            Id = Convert.ToBase64String(data);
            UserId = user.Id;
            Touch(ttl);
        }

        public void Touch(int ttl = 4 * 60 * 60){
            ExpiresAt = DateTime.Now + new TimeSpan(0, 0, ttl);
        }

        /// <summary>
        /// Makes the token expired, by setting the expiry date to something in the past.
        /// </summary>
        public void Expire()
        {
            this.ExpiresAt = DateTime.Now.AddYears(-1);
        }
    }
}
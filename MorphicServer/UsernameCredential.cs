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
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    public struct UsernameCredential: Credential
    {
        [BsonId]
        public string Id { get; set; }
        public string? UserId { get; set; }
        public int? PasswordIterationCount { get; set; }
        public string? PasswordFunction { get; set; }
        public string? PasswordSalt { get; set; }
        public string? PasswordHash { get; set; }

        public void SavePassword(string password)
        {
            PasswordFunction = "SHA512";
            PasswordSalt = RandomSalt();
            PasswordIterationCount = 10000;
            PasswordHash = DerivedPasswordHash(password);
        }

        public bool IsValidPassword(string password)
        {
            var hash = DerivedPasswordHash(password);
            return hash == PasswordHash;
        }

        private string DerivedPasswordHash(string password)
        {
            KeyDerivationPrf function;
            int keyLength;

            if (PasswordFunction == "SHA256")
            {
                function = KeyDerivationPrf.HMACSHA256;
                keyLength = 32;
            }else if (PasswordFunction == "SHA512")
            {
                function = KeyDerivationPrf.HMACSHA512;
                keyLength = 64;
            }else
            {
                throw new Exception("Invalid Key Derivation Function");
            }
            if (PasswordSalt == null){
                throw new Exception("Missing Salt");
            }
            if (PasswordIterationCount == null){
                throw new Exception("Missing Iteration Count");
            }
            var salt = Convert.FromBase64String(PasswordSalt);
            var hash = KeyDerivation.Pbkdf2(password, salt, function, (int)PasswordIterationCount, keyLength);
            return Convert.ToBase64String(hash);
        }

        private static string RandomSalt()
        {
            var salt = new byte[16];
            var provider = RandomNumberGenerator.Create();
            provider.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }
    }

    public static class UsernameCredentialDatabase
    {
        public static async Task<User?> UserForUsername(this Database db, string username, string password)
        {
            var credential = await db.Get<UsernameCredential>(username);
            if (credential?.UserId is string userId)
            {
                if (credential?.IsValidPassword(password) ?? false)
                {
                    return await db.Get<User>(userId);
                }
            }
            return null;
        }
    }
}
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
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace MorphicServer
{
    public class HashedData
    {
        public int IterationCount;
        public string HashFunction;
        public string Salt;
        public string Hash;

        public HashedData(string data, string? salt = null)
        {
            HashFunction = "SHA512";
            Salt = salt ?? RandomSalt();
            IterationCount = 10000;
            Hash = DerivedHash(data);
        }

        public HashedData(int iterationCount, string hashFunction, string salt, string hash)
        {
            HashFunction = hashFunction;
            Salt = salt;
            IterationCount = iterationCount;
            Hash = hash;
        }

        public override string ToString()
        {
            return $"{IterationCount}:{HashFunction}:{Salt}:{Hash}";
        }
        
        public bool Equals(string data)
        {
            var hash = DerivedHash(data);
            return hash == Hash;
        }

        private string DerivedHash(string data)
        {
            KeyDerivationPrf function;
            int keyLength;

            if (HashFunction == "SHA512")
            {
                function = KeyDerivationPrf.HMACSHA512;
                keyLength = 64;
            }
            else
            {
                throw new Exception("Invalid Key Derivation Function");
            }

            var salt = Convert.FromBase64String(Salt);
            var hash = KeyDerivation.Pbkdf2(data, salt, function, IterationCount, keyLength);
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
}
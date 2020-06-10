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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Morphic.Security
{
    /// <summary>
    /// Class to hash and compare data.
    /// </summary>
    public class HashedData
    {
        /// <summary>
        /// Number of iterations for the hash functions
        /// </summary>
        private readonly int iterationCount;

        /// <summary>
        /// The hash function to use. Currently supported: see Pbkdf2Sha512
        /// </summary>
        private readonly string hashFunction;

        /// <summary>
        /// Salt to add to the hashing (google Rainbow Tables)
        /// </summary>
        private readonly string salt;

        /// <summary>
        /// The hashed data
        /// </summary>
        private string hash;

        private const String Pbkdf2Sha512 = "PBKDF2-SHA512";
        private const int IterationCountPbkdf2 = 10000;

        class HashedDataException : Exception
        {
            public HashedDataException(String error) : base(error)
            {
            }
        }

        /// <summary>
        /// Create a HashedData object from data and optionally salt.
        /// </summary>
        /// <param name="data">the data to hash</param>
        /// <param name="salt">(Optional) If not provided, a random salt will be created</param>
        /// <returns></returns>
        public HashedData(string data, string? salt = null)
        {
            this.salt = salt ?? RandomSalt();
            iterationCount = IterationCountPbkdf2;
            hashFunction = Pbkdf2Sha512;
            hash = DoHash(data);
        }

        protected HashedData(int iterationCount, string hashFunction, string salt, string hash)
        {
            this.hashFunction = hashFunction;
            this.salt = salt;
            this.iterationCount = iterationCount;
            this.hash = hash;
        }

        protected static HashedData FromCombinedString(String hashedCombinedString)
        {
            var parts = hashedCombinedString.Split(":");
            if (parts.Length != 4)
            {
                throw new HashedDataException("combined string does not have enough parts");
            }

            int iterations = Int32.Parse(parts[1]);
            return new HashedData(iterations, parts[0], parts[2], parts[3]);
        }

        public string ToCombinedString()
        {
            return $"{hashFunction}:{iterationCount}:{salt}:{hash}";
        }

        public bool Equals(string data)
        {
            return DoHash(data) == hash;
        }

        private string DoHash(string data)
        {
            KeyDerivationPrf function;
            int keyLength;

            if (hashFunction == Pbkdf2Sha512)
            {
                function = KeyDerivationPrf.HMACSHA512;
                keyLength = 64;
            }
            else
            {
                throw new Exception("Invalid Key Derivation Function");
            }

            var s = Convert.FromBase64String(salt);
            var h = KeyDerivation.Pbkdf2(data, s, function, iterationCount, keyLength);
            return Convert.ToBase64String(h);
        }

        private static string RandomSalt()
        {
            var salt = new byte[16];
            var provider = RandomNumberGenerator.Create();
            provider.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Custom Bson Serializer that converts a HashedData to and from its combined string representation
        /// </summary>
        public class BsonSerializer: SerializerBase<HashedData>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, HashedData value)
            {
                context.Writer.WriteString(value.ToCombinedString());
            }

            public override HashedData Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return HashedData.FromCombinedString(context.Reader.ReadString());
            }
        }

        public static bool operator==(HashedData a, string b)
        {
            return a.ToCombinedString() == b;
        }

        public static bool operator!=(HashedData a, string b)
        {
            return a.ToCombinedString() != b;
        }

        public override bool Equals(object? other)
        {
            if (other is HashedData h)
            {
                return this.ToCombinedString() == h.ToCombinedString();
            }
            if (other is string hashString){
                return this == hashString;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToCombinedString().GetHashCode();
        }
    }

    /// <summary>
    /// Represents a string that is stored hashed in the database and only decrypted when needed,
    /// yet is searchable.
    /// </summary>
    public class SearchableHashedString : HashedData
    {
        class SearchableHashedStringException : Exception
        {
            public SearchableHashedStringException(String error) : base(error)
            {
            }
        }

        /// <summary>
        /// We use a statically configured "shared salt".
        /// Why do we need a shared salt? We need to be able to search
        /// for the value. If we use random salt for every entry this becomes prohibitively expensive 
        /// (that being the sole purpose of Salt, after all). This is a trade-off between protecting
        /// PII and searchability: It's not perfect, but it's sufficient. 
        /// </summary>

        public SearchableHashedString(string plaintext) : base(plaintext, Convert.ToBase64String(KeyStorage.Shared.GetPrimaryHashSalt().KeyData))
        {
        }

        private SearchableHashedString(int iterationCount, string hashFunction, string salt, string hash) : base(
            iterationCount, hashFunction, salt, hash)
        {
        }

        // TODO Can't figure out how to get the base class to handle this.
        public new static SearchableHashedString FromCombinedString(String hashedCombinedString)
        {
            var parts = hashedCombinedString.Split(":");
            if (parts.Length != 4)
            {
                throw new SearchableHashedStringException("combined string does not have enough parts");
            }

            int iterations = Int32.Parse(parts[1]);
            return new SearchableHashedString(iterations, parts[0], parts[2], parts[3]);
        }

        public new class BsonSerializer : SerializerBase<SearchableHashedString>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SearchableHashedString value)
            {
                context.Writer.WriteString(value.ToCombinedString());
            }

            public override SearchableHashedString Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return FromCombinedString(context.Reader.ReadString());
            }
        }
    }
}
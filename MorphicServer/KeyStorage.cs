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
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MorphicServer
{

    public class KeyStorageSettings
    {
        //Create keys with: openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key
        public string EncryptionKeyPrimaryEnvName { get; set; } = "MORPHIC_ENC_KEY_PRIMARY";
        public string EncryptionKeyRolloverPrefixEnvName { get; set; } = "MORPHIC_ENC_KEY_ROLLOVER_";
        // Create hash with: openssl rand -hex 16
        public string HashSaltPrimaryEnvName { get; set; } = "MORPHIC_HASH_SALT_PRIMARY";
    }

    public class KeyStorage
    {

        public static KeyStorage CreateShared(KeyStorageSettings options, ILogger<KeyStorage> logger)
        {
            Shared = new KeyStorage(options, logger);
            return Shared;
        }

        public static KeyStorage Shared { get; set; } = null!;

        public KeyStorage(KeyStorageSettings options, ILogger<KeyStorage> logger)
        {
            EncryptionKeyPrimary = options.EncryptionKeyPrimaryEnvName;
            EncryptionKeyRolloverPrefix = options.EncryptionKeyRolloverPrefixEnvName;
            HashSaltPrimary = options.HashSaltPrimaryEnvName;
            this.logger = logger;
        }

        private readonly ILogger<KeyStorage> logger;

        //Create keys with: openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key
        public string EncryptionKeyPrimary { get; }
        public string EncryptionKeyRolloverPrefix { get; }

        // Create hash with: openssl rand -hex 16
        public string HashSaltPrimary { get; }

        private List<KeyInfo>? keyArray;
        private List<KeyInfo>? hashSaltArray;
        
        public KeyInfo GetKey(string keyName)
        {
            LoadKeysFromEnvIfNeeded();
            if (keyArray == null) throw new KeysNotInitialized();
            var key = keyArray.FirstOrDefault(k => k.KeyName == keyName);
            if (key == null) throw new KeyNotFoundException(keyName);
            return key;
        }

        public KeyInfo GetPrimary()
        {
            LoadKeysFromEnvIfNeeded();
            if (keyArray == null) throw new KeysNotInitialized();
            var key = keyArray.FirstOrDefault(k => k.IsPrimary);
            if (key == null) throw new KeyNotFoundException("PRIMARY");
            return key;
        }

        public KeyInfo GetPrimaryHashSalt()
        {
            LoadKeysFromEnvIfNeeded();
            if (hashSaltArray == null) throw new KeysNotInitialized();
            var key = hashSaltArray.FirstOrDefault(k => k.IsPrimary);
            if (key == null) throw new KeyNotFoundException("PRIMARY_HASH");
            return key;
        }
        
        /// <summary>
        /// Clear the keys and re-read. Mostly useful for testing right now, but may be useful in the future.
        /// </summary>
        public void ClearKeys()
        {
            keyArray = null; // TODO Do I need to dispose this?
            hashSaltArray = null; // TODO Do I need to dispose this?
        }
        
        public void LoadKeysFromEnvIfNeeded()
        {
            if (keyArray == null)
            {
                var myKeyArray = new List<KeyInfo>();

                string keyValue = Environment.GetEnvironmentVariable(EncryptionKeyPrimary) ?? "";
                if (String.IsNullOrWhiteSpace(keyValue))
                {
                    throw new EmptyKey(EncryptionKeyPrimary);
                }

                var key = KeyInfoFromEnvValue(EncryptionKeyPrimary, keyValue, true);
                myKeyArray.Add(key);

                // Look for the rollover keys. We allow multiple to give us time to move off of keys
                foreach (var envKey in Environment.GetEnvironmentVariables().Keys)
                {
                    if (envKey == null || !envKey.ToString()!.StartsWith(EncryptionKeyRolloverPrefix))
                        continue;

                    keyValue = Environment.GetEnvironmentVariable(envKey!.ToString() ?? "") ?? "";
                    if (String.IsNullOrWhiteSpace(keyValue) || String.IsNullOrWhiteSpace(keyValue))
                    {
                        continue;
                    }

                    key = KeyInfoFromEnvValue(envKey.ToString()!, keyValue, false);
                    if (myKeyArray.FirstOrDefault(k => k.KeyName == key.KeyName) != null)
                    {
                        throw new DuplicateKey(key.KeyName);
                    }

                    myKeyArray.Add(key);
                }

                keyArray = myKeyArray;
            }

            if (hashSaltArray == null)
            {
                var myHashSaltArray = new List<KeyInfo>();

                // Get the hash salt
                var hashValue = Environment.GetEnvironmentVariable(HashSaltPrimary) ?? "";
                if (String.IsNullOrWhiteSpace(hashValue))
                {
                    throw new EmptyKey(HashSaltPrimary);
                }

                var hash = KeyInfoFromEnvValue(HashSaltPrimary, hashValue, true);

                myHashSaltArray.Add(hash);
                hashSaltArray = myHashSaltArray;
                logger.LogDebug($"Loaded {hashSaltArray.Count} hash-salts");
            }
        }

        private KeyInfo KeyInfoFromEnvValue(string keySourceName, string keyString, bool isPrimary)
        {
            var parts = keyString.Split(":");
            if (parts.Length != 2)
            {
                throw new BadKeyFormat(keySourceName);
            }
            return new KeyInfo(parts[0], HexStringToBytes(parts[1]), isPrimary);
        }
        
        public static byte[] HexStringToBytes(string hexString)
        {
            if (String.IsNullOrWhiteSpace(hexString))
            {
                throw new HexStringFormatException("hexString is empty");
            }

            if (hexString.Length % 2 != 0)
            {
                throw new HexStringFormatException("hexString must have an even length");
            }

            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                try
                {
                    string currentHex = hexString.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(currentHex, 16);
                }
                catch (FormatException)
                {
                    throw new HexStringFormatException("Bad characters in key data");
                }
            }
            return bytes;
        }

        public class KeyInfo
        {
            public readonly byte[] KeyData;
            public readonly string KeyName;
            public readonly bool IsPrimary; 

            public KeyInfo(string keyName, byte[] keyData, bool isPrimary)
            {
                KeyName = keyName;
                KeyData = keyData;
                IsPrimary = isPrimary;
            }
        }

        public class KeyNotFoundException : Exception
        {
            public KeyNotFoundException(string error) : base(error)
            {
            }
        }

        public class KeysNotInitialized : Exception
        {
        }

        public class DuplicateKey : Exception
        {
            public DuplicateKey(string error) : base(error)
            {
                
            }
        }
        public class EmptyKey : Exception
        {
            public EmptyKey(string error) : base(error)
            {
                
            }
        }

        public class BadKeyFormat : Exception
        {
            public BadKeyFormat(string error) : base(error)
            {
                
            }
        }
        
        public class HexStringFormatException : Exception
        {
            public HexStringFormatException(string error) : base(error)
            {
            }
        }
    }
}
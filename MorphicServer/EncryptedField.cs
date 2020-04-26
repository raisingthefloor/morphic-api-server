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
using System.IO;
using System.Security.Cryptography;

namespace MorphicServer
{
    public class EncryptedField
    {
        private const string Aes256CbcString = "AES-256-CBC";

        public EncryptedField(string keyName, string cipher, string iv, string cipherText)
        {
            KeyName = keyName;
            Cipher = cipher;
            Iv = iv;
            CipherText = cipherText;
        }

        public string KeyName { get; }
        public string Cipher { get; }
        public string Iv { get; }
        public string CipherText { get; }

        public static EncryptedField FromPlainText(string plainText)
        {
            var iv = RandomIv();
            var key = KeyStorage.GetPrimary();
            var encryptedData = new EncryptedField(
                key.KeyName,
                Aes256CbcString,
                iv,
                Convert.ToBase64String(
                    EncryptStringToBytes_Aes256CBC(
                        plainText,
                        key.KeyData, // we always encrypt with the primary
                        Convert.FromBase64String(iv))));
            return encryptedData;
        }

        public static EncryptedField FromCombinedString(string combinedString)
        {
            var parts = combinedString.Split(":");
            var encryptedField = new EncryptedField(
                parts[0],
                parts[1],
                parts[2],
                parts[3]);
            return encryptedField;
        }

        public string ToCombinedString()
        {
            return $"{KeyName}:{Cipher}:{Iv}:{CipherText}";
        }

        /// <summary>
        /// Decrypt the data in the EncryptedField class.
        /// </summary>
        /// <param name="isPrimary">Indicates whether the text was encrypted with the primary key or not.
        /// Caller should re-encrypt with the primary key if this is returned false</param>
        /// <returns>the plainText string</returns>
        /// <exception cref="UnknownCipherModeException"></exception>
        public string Decrypt(out bool isPrimary)
        {
            if (Cipher == Aes256CbcString)
            {
                var keyInfo = KeyStorage.GetKey(KeyName); 
                isPrimary = keyInfo.IsPrimary;
                var plainText = DecryptStringFromBytes_Aes256CBC(
                    Convert.FromBase64String(CipherText),
                    keyInfo.KeyData,
                    Convert.FromBase64String(Iv));
                return plainText;
            }

            throw new UnknownCipherModeException(Cipher);
        }

        public class EncryptedFieldException : Exception
        {
            protected EncryptedFieldException(string error) : base(error)
            {
            }
        }
        public class PlainTextEmptyException : EncryptedFieldException
        {
            public PlainTextEmptyException(string error) : base(error)
            {
            }
        }

        public class CipherTextEmptyException : EncryptedFieldException
        {
            public CipherTextEmptyException(string error) : base(error)
            {
            }
        }

        public class KeyArgumentBad : EncryptedFieldException
        {
            public KeyArgumentBad(string error) : base(error)
            {
            }
        }

        public class IvArgumentBad : EncryptedFieldException
        {
            public IvArgumentBad(string error) : base(error)
            {
            }
        }

        private static byte[] EncryptStringToBytes_Aes256CBC(string plainText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new PlainTextEmptyException("plainText");
            if (key == null || key.Length <= 16)
                throw new KeyArgumentBad("key");
            if (iv == null || iv.Length <= 0)
                throw new IvArgumentBad("iv");
            byte[] encrypted;

            // Create an AesCryptoServiceProvider object
            // with the specified key and IV.
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                // Create an encryptor to perform the stream transform.
                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        private static string DecryptStringFromBytes_Aes256CBC(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new CipherTextEmptyException("cipherText");
            if (key == null || key.Length <= 0)
                throw new KeyArgumentBad("key");
            if (iv == null || iv.Length <= 0)
                throw new IvArgumentBad("iv");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            // Create an AesCryptoServiceProvider object
            // with the specified key and IV.
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;

                // Create a decryptor to perform the stream transform.
                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private static string RandomIv()
        {
            var iv = new byte[16];
            var provider = RandomNumberGenerator.Create();
            provider.GetBytes(iv);
            return Convert.ToBase64String(iv);
        }

        private class UnknownCipherModeException : EncryptedFieldException
        {
            public UnknownCipherModeException(string error) : base(error)
            {
            }
        }
    }
}
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
using Xunit;
using Microsoft.Extensions.Logging;

namespace Morphic.Security.Tests
{
    public class EncryptedFieldsTests: IDisposable
    {
        //Create keys with:
        // $ openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key
        
        // TODO because of the singleton nature of KeyStorage.Shared, all the tests have to be in this file/class. Need to refactor/rethink KeyStorage.Shared
        public EncryptedFieldsTests()
        {
            var settings = new KeyStorageSettings();
            var logger = new LoggerFactory().CreateLogger<KeyStorage>();
            KeyStorage.Shared = new KeyStorage(settings, logger);
        }

        public void Dispose()
        {
            KeyStorage.Shared = null!;
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", null);
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_2", null);
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", null);
        }

        [Fact]
        public void TestKeyLoading()
        {
            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);

            var oddKeyName = "ODD_NUMBER_LETTERS";
            var oddKeyData = "123";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{oddKeyName}:{oddKeyData}");
            Assert.Throws<KeyStorage.HexStringFormatException>(() => KeyStorage.Shared.GetPrimary());
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            
            var badKeyName = "BAD_KEY";
            var badKeyData = "ThisIsNotAKey/1234";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{badKeyName}:{badKeyData}");
            Assert.Throws<KeyStorage.HexStringFormatException>(() => KeyStorage.Shared.GetKey(badKeyName));
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{badKeyData}");
            Assert.Throws<KeyStorage.BadKeyFormat>(() => KeyStorage.Shared.GetKey(badKeyName));
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);

            var keyName = "TEST_KEY";
            var keyData = "8C532F0C2CCE7AF471111285340B6353FCB327DF9AB9F0121731F403E3FFDC7C";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{keyName}:{keyData}");

            var rolloverKeyName1 = "SomeKey";
            var rolloverKeyData1 = "12FE1D86B4849B34FC1C950E671284BC30DA751E3331C0F36F15F7F51C7922D8";
            var rolloverKeyName2 = "SomeKey2";
            var rolloverKeyData2 = "05A2D69574BE13264E1BAB68453CBCF99A7A5C88243807613C8184BE38115BB9";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", $"{rolloverKeyName1}:{rolloverKeyData1}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_2", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");

            // success: make sure we get the primary back
            var key = KeyStorage.Shared.GetKey(keyName);
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), key.KeyData);
            Assert.True(key.IsPrimary);
            key = KeyStorage.Shared.GetKey(rolloverKeyName1);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), key.KeyData);
            Assert.False(key.IsPrimary);
            key = KeyStorage.Shared.GetKey(rolloverKeyName2);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), key.KeyData);
            Assert.False(key.IsPrimary);
            Assert.Throws<KeyStorage.KeyNotFoundException>(() => KeyStorage.Shared.GetKey("Unknown_key"));
        }
        
        public EncryptedField AssertProperlyEncrypted(string keyName, string plainText)
        {
            var encryptedField = EncryptedField.FromPlainText(plainText);
            Assert.NotNull(encryptedField);
            Assert.Equal("AES-256-CBC", encryptedField.Cipher);
            Assert.NotEqual("", encryptedField.Iv);
            Assert.Equal(16, Convert.FromBase64String(encryptedField.Iv).Length);
            Assert.Equal(keyName, encryptedField.KeyName);
            Assert.NotEqual(plainText, Convert.FromBase64String(encryptedField.CipherText).ToString());
            return encryptedField;
        }
        
        [Fact]
        public void TestEncryptedField()
        {
            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");

            var keyName = "TEST_KEY";
            var keyData = "8C532F0C2CCE7AF471111285340B6353FCB327DF9AB9F0121731F403E3FFDC7C";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{keyName}:{keyData}");
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), KeyStorage.Shared.GetPrimary().KeyData);
    
            string plainText = "thequickbrownfoxjumpedoverthelazydog";
            var encryptedField = AssertProperlyEncrypted(keyName, plainText);

            string decryptedText = encryptedField.Decrypt();
            Assert.Equal(plainText, decryptedText);

            var otherEncryptedField = EncryptedField.FromCombinedString(encryptedField.ToCombinedString());
            decryptedText = otherEncryptedField.Decrypt();
            Assert.Equal(plainText, decryptedText);

            AssertProperlyEncrypted(keyName, "");
        }

        [Fact]
        public void TestRolloverEncryption()
        {
            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");

            string plainText = "thequickbrownfoxjumpedoverthelazydog";
            string plainText_1 = "thequickbrownfoxjumpedoverthelazydog_1";
            string plainText_2 = "thequickbrownfoxjumpedoverthelazydog_2";
            var keyName = "TEST_KEY";
            var keyData = "8C532F0C2CCE7AF471111285340B6353FCB327DF9AB9F0121731F403E3FFDC7C";
            var rolloverKeyName1 = "SomeKey";
            var rolloverKeyData1 = "12FE1D86B4849B34FC1C950E671284BC30DA751E3331C0F36F15F7F51C7922D8";
            var rolloverKeyName2 = "SomeKey2";
            var rolloverKeyData2 = "05A2D69574BE13264E1BAB68453CBCF99A7A5C88243807613C8184BE38115BB9";
            
            // First, let's start encrypting with a future rollover key.
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.Shared.GetPrimary().KeyData);
            
            var encryptedFieldRoll2 = AssertProperlyEncrypted(rolloverKeyName2, plainText_2);
            string decryptedText = encryptedFieldRoll2.Decrypt();
            Assert.Equal(plainText_2, decryptedText);


            // we move the previous key to rollover
            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{rolloverKeyName1}:{rolloverKeyData1}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), KeyStorage.Shared.GetPrimary().KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.Shared.GetKey(rolloverKeyName2).KeyData);
            
            var encryptedFieldRoll1 = AssertProperlyEncrypted(rolloverKeyName1, plainText_1);
            decryptedText = encryptedFieldRoll1.Decrypt();
            Assert.Equal(plainText_1, decryptedText);

            // when decrypting, the key used is no longer the primary.
            decryptedText = encryptedFieldRoll2.Decrypt();
            Assert.Equal(plainText_2, decryptedText);
            
            // now we switch to the 'new' primary key, and other rollovers
            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", $"{rolloverKeyName1}:{rolloverKeyData1}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_2", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{keyName}:{keyData}");
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), KeyStorage.Shared.GetPrimary().KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), KeyStorage.Shared.GetKey(rolloverKeyName1).KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.Shared.GetKey(rolloverKeyName2).KeyData);

            var encryptedField = AssertProperlyEncrypted(keyName, plainText);
            decryptedText = encryptedField.Decrypt();
            Assert.Equal(plainText, decryptedText);

            decryptedText = encryptedFieldRoll1.Decrypt();
            Assert.Equal(plainText_1, decryptedText);

            decryptedText = encryptedFieldRoll2.Decrypt();
            Assert.Equal(plainText_2, decryptedText);
        }
        
                [Fact]
        public void TestHashedData()
        {
            // set the Salt, but it won't be used in this test. Make sure it doesn't.
            var testString = "thequickbrownfoxjumpedoverthelazydog";
            var hash = new HashedData(testString);
            var hashDbString = hash.ToCombinedString();
            Assert.NotNull(hashDbString);
            Assert.DoesNotContain("SALT1", hashDbString);
            Assert.Equal(4, hashDbString.Split(":").Length);
            Assert.True(hash.Equals(testString));
            Assert.False(hash.Equals(testString+"123"));
            
            Assert.Equal(hash.ToCombinedString(),
                HashedData.FromCombinedString(hash.ToCombinedString()).ToCombinedString());

        }

        [Fact]
        public void TestSearchableHashedString()
        {
            // set the Salt. It will be used in this test.
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");
            var saltAsB64 = "Nh5mXvN4qwYDGAZGm3h5vQ==";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", "ENCKEY:CE2BED7EF7A3871AD87EE80116D360A9FA368B6A7790E9D0D4D314ED83B9AB5E");
            var testString = "thequickbrownfoxjumpedoverthelazydog";
            var searchHash = new SearchableHashedString(testString);
            var hashDbString = searchHash.ToCombinedString();
            Assert.NotNull(hashDbString);
            Assert.Contains(saltAsB64, hashDbString);

            var testString2 = testString + testString;
            var searchHash2 = new SearchableHashedString(testString2);
            var hashDbString2 = searchHash2.ToCombinedString();
            Assert.NotNull(hashDbString2);
            Assert.False(searchHash.Equals(testString2));
            Assert.True(searchHash2.Equals(testString2));

            Assert.Equal(searchHash2.ToCombinedString(),
                SearchableHashedString.FromCombinedString(searchHash2.ToCombinedString()).ToCombinedString());
        }

        [Fact]
        public void TestEncryptedString()
        {
            Assert.NotNull(KeyStorage.Shared);

            KeyStorage.Shared.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", "TEST_KEY:8C532F0C2CCE7AF471111285340B6353FCB327DF9AB9F0121731F403E3FFDC7C");
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");
            Assert.NotNull(KeyStorage.Shared);

            string plainText = "thequickbrownfoxjumpedoverthelazydog";
            Assert.NotNull(KeyStorage.Shared);
            var encrypted = new SearchableEncryptedString(plainText);
            Assert.NotNull(KeyStorage.Shared);
            Assert.NotNull(encrypted.Hash);
            Assert.NotNull(KeyStorage.Shared);
            Assert.NotNull(encrypted.Encrypted);
            Assert.NotNull(KeyStorage.Shared);
            Assert.Equal(plainText, encrypted.PlainText);
        }
    }
}
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

namespace MorphicServer.Tests
{
    public class EncryptedFieldsTests
    {
        //Create keys with:
        // $ openssl enc -aes-256-cbc -k <somepassphrase> -P -md sha1 | grep key

        [Fact]
        public void TestKeyLoading()
        {
            KeyStorage.ClearKeys();
            
            var oddKeyName = "ODD_NUMBER_LETTERS";
            var oddKeyData = "123";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{oddKeyName}:{oddKeyData}");
            Assert.Throws<KeyStorage.HexStringFormatException>(() => KeyStorage.GetPrimary());
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            
            var badKeyName = "BAD_KEY";
            var badKeyData = "ThisIsNotAKey/1234";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{badKeyName}:{badKeyData}");
            Assert.Throws<KeyStorage.HexStringFormatException>(() => KeyStorage.GetKey(badKeyName));
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{badKeyData}");
            Assert.Throws<KeyStorage.BadKeyFormat>(() => KeyStorage.GetKey(badKeyName));
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

            // success: make sure we get the primary back
            var key = KeyStorage.GetKey(keyName);
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), key.KeyData);
            Assert.True(key.IsPrimary);
            key = KeyStorage.GetKey(rolloverKeyName1);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), key.KeyData);
            Assert.False(key.IsPrimary);
            key = KeyStorage.GetKey(rolloverKeyName2);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), key.KeyData);
            Assert.False(key.IsPrimary);
            Assert.Throws<KeyStorage.KeyNotFoundException>(() => KeyStorage.GetKey("Unknown_key"));
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", null);
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_2", null);
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
        public void TestEncryption()
        {
            KeyStorage.ClearKeys();

            bool isPrimary;
            var keyName = "TEST_KEY";
            var keyData = "8C532F0C2CCE7AF471111285340B6353FCB327DF9AB9F0121731F403E3FFDC7C";
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{keyName}:{keyData}");
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), KeyStorage.GetPrimary().KeyData);
    
            string plainText = "thequickbrownfoxjumpedoverthelazydog";
            var encryptedField = AssertProperlyEncrypted(keyName, plainText);

            string decryptedText = encryptedField.Decrypt(out isPrimary);
            Assert.True(isPrimary);
            Assert.Equal(plainText, decryptedText);

            var otherEncryptedField = EncryptedField.FromCombinedString(encryptedField.ToCombinedString());
            decryptedText = otherEncryptedField.Decrypt(out isPrimary);
            Assert.True(isPrimary);
            Assert.Equal(plainText, decryptedText);

            Assert.Throws<EncryptedField.PlainTextEmptyException>(
                () => EncryptedField.FromPlainText(""));
            
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
        }

        [Fact]
        public void TestRolloverEncryption()
        {
            KeyStorage.ClearKeys();

            bool isPrimary;
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
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.GetPrimary().KeyData);
            
            var encryptedFieldRoll2 = AssertProperlyEncrypted(rolloverKeyName2, plainText_2);
            string decryptedText = encryptedFieldRoll2.Decrypt(out isPrimary);
            Assert.Equal(plainText_2, decryptedText);
            Assert.True(isPrimary);


            // we move the previous key to rollover
            KeyStorage.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{rolloverKeyName1}:{rolloverKeyData1}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), KeyStorage.GetPrimary().KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.GetKey(rolloverKeyName2).KeyData);
            
            var encryptedFieldRoll1 = AssertProperlyEncrypted(rolloverKeyName1, plainText_1);
            decryptedText = encryptedFieldRoll1.Decrypt(out isPrimary);
            Assert.Equal(plainText_1, decryptedText);
            Assert.True(isPrimary);

            // when decrypting, the key used is no longer the primary.
            decryptedText = encryptedFieldRoll2.Decrypt(out isPrimary);
            Assert.False(isPrimary);
            Assert.Equal(plainText_2, decryptedText);
            
            // now we switch to the 'new' primary key, and other rollovers
            KeyStorage.ClearKeys();
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_1", $"{rolloverKeyName1}:{rolloverKeyData1}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_ROLLOVER_2", $"{rolloverKeyName2}:{rolloverKeyData2}");
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", $"{keyName}:{keyData}");
            Assert.Equal(KeyStorage.HexStringToBytes(keyData), KeyStorage.GetPrimary().KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData1), KeyStorage.GetKey(rolloverKeyName1).KeyData);
            Assert.Equal(KeyStorage.HexStringToBytes(rolloverKeyData2), KeyStorage.GetKey(rolloverKeyName2).KeyData);

            var encryptedField = AssertProperlyEncrypted(keyName, plainText);
            decryptedText = encryptedField.Decrypt(out isPrimary);
            Assert.Equal(plainText, decryptedText);
            Assert.True(isPrimary);

            decryptedText = encryptedFieldRoll1.Decrypt(out isPrimary);
            Assert.False(isPrimary);
            Assert.Equal(plainText_1, decryptedText);

            decryptedText = encryptedFieldRoll2.Decrypt(out isPrimary);
            Assert.False(isPrimary);
            Assert.Equal(plainText_2, decryptedText);

            
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", null);
        }
    }
}
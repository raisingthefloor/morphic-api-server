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
using System.Text.Json;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    
    /// <summary>
    /// Represents a string that is stored encrypted in the database and only decrypted when needed
    /// </summary>
    public class EncryptedString
    {

        public EncryptedField? Encrypted { get; set; }

        [BsonIgnore]
        public virtual string? PlainText
        {
            get{
                if (Encrypted is EncryptedField encrypted)
                {
                    return encrypted.Decrypt();
                }
                return null;
            }

            set{
                if (value is string plainText)
                {
                    Encrypted = EncryptedField.FromPlainText(plainText);
                }
                else
                {
                    Encrypted = null;
                }
            }
        }

        public class JsonConverter: System.Text.Json.Serialization.JsonConverter<EncryptedString>
        {
            public override EncryptedString Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var plainText = reader.GetString();
                var encrypted = new EncryptedString();
                encrypted.PlainText = plainText;
                return encrypted;
            }

            public override void Write (Utf8JsonWriter writer, EncryptedString value, JsonSerializerOptions options)
            {
                if (value.PlainText is string plainText)
                {
                    writer.WriteStringValue(plainText);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

    }

    /// <summary>
    /// Represents a string that is stored encrypted in the database and only decrypted when needed,
    /// yet is searchable via a hash representation
    /// </summary>
    public class SearchableEncryptedString: EncryptedString
    {

        public SearchableEncryptedString()
        {
        }

        public SearchableEncryptedString(string plaintext)
        {
            PlainText = plaintext;
        }
        
        public SearchableHashedString? Hash { get; set; }

        [BsonIgnore]
        public override string? PlainText{
            get{
                return base.PlainText;
            }

            set{
                base.PlainText = value;
                if (value is string plainText)
                {
                    Hash = new SearchableHashedString(plainText);
                }
                else
                {
                    Hash = null;
                }
            }
        }

        public new class JsonConverter: System.Text.Json.Serialization.JsonConverter<SearchableEncryptedString>
        {
            public override SearchableEncryptedString Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var plainText = reader.GetString();
                var encrypted = new SearchableEncryptedString();
                encrypted.PlainText = plainText;
                return encrypted;
            }

            public override void Write (Utf8JsonWriter writer, SearchableEncryptedString value, JsonSerializerOptions options)
            {
                if (value.PlainText is string plainText)
                {
                    writer.WriteStringValue(plainText);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }

    }
}
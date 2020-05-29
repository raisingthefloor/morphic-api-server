using System;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            SharedSalt = Convert.ToBase64String(KeyStorage.Shared.GetPrimaryHashSalt().KeyData);
        }

        public HashedData? Hash { get; set; }

        /// <summary>
        /// Why do we need a shared salt? We need to be able to search
        /// for the value. If we use random salt for every entry this becomes prohibitively expensive 
        /// (that being the sole purpose of Salt, after all). This is a trade-off between protecting
        /// PII and searchability: It's not perfect, but it's sufficient. 
        /// </summary>
        [BsonIgnore]
        public string SharedSalt { get; set; }

        [BsonIgnore]
        public override string? PlainText{
            get{
                return base.PlainText;
            }

            set{
                base.PlainText = value;
                if (value is string plainText)
                {
                    Hash = HashedData.FromString(plainText, SharedSalt);
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
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
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Serilog;

namespace MorphicServer
{
    public class User: Record
    {
        [JsonIgnore]
        public string? EmailHash { get; set; }
        [JsonIgnore]
        public string? EmailEncrypted { get; set; }
        [JsonIgnore]
        public bool EmailVerified { get; set; }
        
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        [JsonPropertyName("preferences_id")]
        public string? PreferencesId { get; set; }
        [JsonIgnore]
        public DateTime LastAuth { get; set; }

        [JsonPropertyName("email")]
        [BsonIgnore]
        public string? Email {
            get{
                if (EmailEncrypted is string encrypted)
                {
                    var field = EncryptedField.FromCombinedString(EmailEncrypted);
                    var email = field.Decrypt(out var isPrimary);
                    if (!isPrimary){
                        // The encryption key used is not the primary key. It's an older one.
                        // This means we need to re-encrypt the data and save it back to the DB
                        // TODO implement key-rollover background task
                        Log.Logger.Error("TODO Need to re-encrypt with primary in background");
                    }
                    return email;
                }
                return null;
            }
            set
            {
                if (value is string email)
                {
                    if ((EmailHash is string existingHash) && HashedData.FromCombinedString(existingHash).Equals(email))
                    {
                        return;
                    }
                    EmailHash = HashedData.FromString(email, DefaultUserEmailSalt).ToCombinedString();
                    EmailEncrypted = EncryptedField.FromPlainText(email).ToCombinedString();
                    EmailVerified = false;
                }
                else
                {
                    EmailHash = null;
                    EmailEncrypted = null;
                }
            }
        }

        public void TouchLastAuth()
        {
            LastAuth = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Default salt for user-email hashing. Why do we need default salt? We need to be able to search
        /// for the email. If we use random salt for every entry this becomes prohibitively expensive 
        /// (that being the sole purpose of Salt, after all). This is a trade-off between protecting
        /// PII and searchability: It's not perfect, but it's sufficient. 
        /// </summary>
        const string DefaultUserEmailSalt = "N9DtOumwMC7A9KJLB3oCbA==";

        public static string UserEmailHashCombined(string email)
        {
            return HashedData.FromString(email, User.DefaultUserEmailSalt).ToCombinedString();
        }

        public string FullnameOrEmail()
        {
            if (FullName == "")
            {
                return Email ?? "";
            }
            else
            {
                return FullName;
            }
        }
        
        [BsonIgnore]
        [JsonIgnore]
        public string FullName
        {
            get
            {
                string fullName = "";

                if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
                {
                    if (!string.IsNullOrEmpty(FirstName)) fullName = FirstName!;
                    if (!string.IsNullOrEmpty(LastName))
                    {
                        if (fullName == "")
                        {
                            fullName = LastName;
                        }
                        else
                        {
                            fullName += " " + LastName;
                        }
                    }
                }

                return fullName;
            }
        }
    }
}
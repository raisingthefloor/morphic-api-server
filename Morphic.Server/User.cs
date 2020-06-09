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
using System.Net.Mail;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using Morphic.Security;

namespace Morphic.Server
{
    public class User: Record
    {
        
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        [JsonPropertyName("preferences_id")]
        public string? PreferencesId { get; set; }
        [JsonPropertyName("email")]
        public SearchableEncryptedString Email { get; set; } = new SearchableEncryptedString();
        [JsonIgnore]
        public bool EmailVerified { get; set; }
        [JsonIgnore]
        public DateTime LastAuth { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString();
        }
        
        public void TouchLastAuth()
        {
            LastAuth = DateTime.UtcNow;
        }

        public string FullnameOrEmail()
        {
            if (FullName == "")
            {
                return Email.PlainText ?? "";
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
        
        public static bool IsValidEmail(string emailAddress)
        {
            try
            {
                // ReSharper disable once ObjectCreationAsStatement
                new MailAddress(emailAddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public static class UserDatabase
    {
        public static async Task<User?> UserForEmail(this Database db, string email, Database.Session? session = null)
        {
            string hash = new SearchableHashedString(email).ToCombinedString();
            return await db.Get<User>(a => a.Email.Hash! == hash, session);
        }
    }
}
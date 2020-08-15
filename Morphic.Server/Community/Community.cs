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


namespace Morphic.Server.Community
{

    using Db;

    public class Community: Record
    {

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("default_bar_id")]
        public string DefaultBarId { get; set; } = null!;

        [JsonPropertyName("billing_id")]
        public string? BillingId { get; set; }

        // Does not include the one free manager everyone is allowed
        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        // Does not include the one free manager everyone is allowed
        [JsonPropertyName("member_limit")]
        public int MemberLimit { get; set; }

        [JsonIgnore]
        public DateTime? LockedDate { get; set; }

        [BsonIgnore]
        [JsonPropertyName("is_locked")]
        public bool IsLocked
        {
            get
            {
                return LockedDate != null;
            }
        }

        [JsonIgnore]
        public DateTime CreatedAt { get; set; }

    }

}
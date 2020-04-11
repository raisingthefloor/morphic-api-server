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

using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{

    /// <summary>Dummy data model for preferences</summary>
    public class Preferences : Record
    {
        public string? UserId { get; set; }

        /// <summary>The user's default preferences</summary>
        // Stored as a serialized JSON string in the mongo database because keys might contain dots,
        // and mongoDB doesn't allow dots in field keys.  Since we're unlikely to need to run queries
        // within the solution preferences, we don't lose any functionality by storing serialized JSON.
        [BsonSerializer(typeof(Database.JsonSerializer<Dictionary<string, SolutionPreferences>>))]
        public Dictionary<string, SolutionPreferences>? Default { get; set; }
    }

    /// <summary>Stores preferences for a specific solution</summary>
    public class SolutionPreferences
    {
        /// <summary>Arbitrary preferences specific to the solution</summary>
        [JsonExtensionData]
        [BsonExtraElements]
        public Dictionary<string, object>? Values { get; set; }
    }
}
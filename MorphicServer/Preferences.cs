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
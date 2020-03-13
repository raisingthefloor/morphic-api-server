using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{

    /// <summary>Dummy data model for preferences</summary>
    public class Preferences : Record
    {
        public string? UserId { get; set; }
    }
}
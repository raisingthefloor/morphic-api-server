using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    public class User: Record
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PreferencesId { get; set; }
    }
}
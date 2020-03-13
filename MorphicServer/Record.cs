using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MorphicServer
{
    public class Record
    {

        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Id { get; set; } = default(Guid);
    }
}
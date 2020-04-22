using System;
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    public struct AuthToken: Record
    {

        [BsonId]
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime ExpiresAt { get; set; }

        public AuthToken(User user, int ttl = 4 * 60 * 60)
        {
            var provider = RandomNumberGenerator.Create();
            var data = new byte[64];
            provider.GetBytes(data);
            Id = Convert.ToBase64String(data);
            UserId = user.Id;
            ExpiresAt = DateTime.Now;
            Touch(ttl);
        }

        public void Touch(int ttl = 4 * 60 * 60){
            ExpiresAt = DateTime.Now + new TimeSpan(0, 0, ttl);
        }
    }
}
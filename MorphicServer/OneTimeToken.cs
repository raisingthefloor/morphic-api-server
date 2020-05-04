using System;
using System.Threading.Tasks;

namespace MorphicServer
{
    public class OneTimeToken : Record
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }

        private const int DefaultExpiresSeconds = 30 * 24 * 60 * 60; // 2592000 seconds in 30 days
        
        public OneTimeToken(string userId, int expiresInSeconds = DefaultExpiresSeconds)
        {
            Id = Guid.NewGuid().ToString();
            UserId = userId;
            Token = newToken();
            ExpiresAt = DateTime.UtcNow + new TimeSpan(0, 0, expiresInSeconds);
        }

        private string newToken()
        {
            return EncryptedField.Random128BitsBase64();
        }
        
        public async Task Invalidate(Database db)
        {
            await db.Delete(this);
        }
    }
}
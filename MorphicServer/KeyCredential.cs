using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    public class KeyCredential: Record
    {
        public string? UserId;
    }

    public static class KeyCredentialDatabase
    {
        public static async Task<User?> UserForKey(this Database db, string key)
        {
            var credential = await db.Get<KeyCredential>(key);
            if (credential == null || credential.UserId == null){
                return null;
            }
            return await db.Get<User>(credential.UserId);
        }
    }
}
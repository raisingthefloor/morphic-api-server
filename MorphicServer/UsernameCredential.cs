using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace MorphicServer
{
    public class UsernameCredential: Record
    {
        public int? PasswordIterationCount;
        public string? PasswordFunction;
        public string? PasswordSalt;
        public string? PasswordHash;
        public string? UserId;

        public void SavePassword(string password)
        {
            PasswordFunction = "SHA512";
            PasswordSalt = RandomSalt();
            PasswordIterationCount = 10000;
            PasswordHash = DerivedPasswordHash(password);
        }

        public bool IsValidPassword(string password)
        {
            var hash = DerivedPasswordHash(password);
            return hash == PasswordHash;
        }

        private string DerivedPasswordHash(string password)
        {
            KeyDerivationPrf function;
            int keyLength;

            if (PasswordFunction == "SHA256")
            {
                function = KeyDerivationPrf.HMACSHA256;
                keyLength = 32;
            }else if (PasswordFunction == "SHA512")
            {
                function = KeyDerivationPrf.HMACSHA512;
                keyLength = 64;
            }else
            {
                throw new Exception("Invalid Key Derivation Function");
            }
            if (PasswordSalt == null){
                throw new Exception("Missing Salt");
            }
            if (PasswordIterationCount == null){
                throw new Exception("Missing Iteration Count");
            }
            var salt = Convert.FromBase64String(PasswordSalt);
            var hash = KeyDerivation.Pbkdf2(password, salt, function, (int)PasswordIterationCount, keyLength);
            return Convert.ToBase64String(hash);
        }

        private static string RandomSalt()
        {
            var salt = new byte[16];
            var provider = RandomNumberGenerator.Create();
            provider.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }
    }

    public static class UsernameCredentialDatabase
    {
        public static async Task<User?> UserForUsername(this Database db, string username, string password)
        {
            var credential = await db.Get<UsernameCredential>(username);
            if (credential == null || credential.UserId == null || !credential.IsValidPassword(password)){
                return null;
            }
            return await db.Get<User>(credential.UserId);
        }
    }
}
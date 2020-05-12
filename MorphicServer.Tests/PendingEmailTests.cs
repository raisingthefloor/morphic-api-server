#nullable enable
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class PendingEmailTests : EndpointTests
    {
        private void AssertPendingValid(User user, string fromEmail, string fromFullname, PendingEmail pending, string subject, string message)
        {
            Assert.NotNull(pending.ToEmailEncr);
            Assert.NotEqual(user.Email, pending.ToEmailEncr);
            Assert.Equal(user.Email, pending.ToEmail);
            Assert.StartsWith("AES-256-CBC", pending.ToEmailEncr);
            
            Assert.NotNull(pending.ToFullNameEncr);
            Assert.NotEqual(user.FullName, pending.ToFullNameEncr);
            Assert.Equal(user.FullName, pending.ToFullName);
            Assert.StartsWith("AES-256-CBC", pending.ToFullNameEncr);
            
            Assert.NotNull(pending.SubjectEncr);
            Assert.NotEqual(subject, pending.SubjectEncr);
            Assert.Equal(subject, pending.Subject);
            Assert.StartsWith("AES-256-CBC", pending.SubjectEncr);
            
            Assert.NotNull(pending.EmailTextEncr);
            Assert.NotEqual(message, pending.EmailTextEncr);
            Assert.Equal(message, pending.EmailText);
            Assert.StartsWith("AES-256-CBC", pending.EmailTextEncr);
            
            Assert.Equal(fromEmail, pending.FromEmail);
            Assert.Equal(fromFullname, pending.FromFullName);
        }
        
        [Fact]
        public async Task TestEncryption()
        {
            var fromEmail = "support@example.com";
            var fromFullname = "Johnny Supportguy";
            
            var userInfo1 = await CreateTestUser();
            var user = await Database.Get<User>(userInfo1.Id);
            Assert.NotNull(user);
            var subject = $"Subject{userInfo1.Id}";
            var message = "The quick brown fox jumped over the lazy dog";
            var pending = new PendingEmail(user!, fromEmail, fromFullname, 
                subject, message, PendingEmail.EmailTypeEnum.EmailValidation);
            AssertPendingValid(user!, fromEmail, fromFullname, pending, subject, message);
            
            var userInfo2 = await CreateTestUser("", "");
            user = await Database.Get<User>(userInfo1.Id);
            Assert.NotNull(user);
            subject = $"Subject{userInfo2.Id}";
            message = "The quick brown fox jumped over the lazy dog too!";
            pending = new PendingEmail(user!, fromEmail, fromFullname,
                subject, message, PendingEmail.EmailTypeEnum.EmailValidation);
            AssertPendingValid(user!, fromEmail, fromFullname, pending, subject, message);
        }
    }
}
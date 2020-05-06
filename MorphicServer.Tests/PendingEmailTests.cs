#nullable enable
using System.Threading.Tasks;
using Xunit;

namespace MorphicServer.Tests
{
    public class EmailTemplateTests
    {
        [Fact]
        public async Task TestGreeting()
        {
            string email = "foo@example.com";
            string? firstName = null;
            string? lastName = null;
            Assert.Equal(email, EmailTemplates.GreetingName(firstName, lastName, email));

            firstName = "foo";
            Assert.Equal($"{firstName}", EmailTemplates.GreetingName(firstName, lastName, email));

            firstName = null;
            lastName = "bar";
            Assert.Equal($"{lastName}", EmailTemplates.GreetingName(firstName, lastName, email));

            firstName = "foo";
            lastName = "bar";
            Assert.Equal($"{firstName} {lastName}", EmailTemplates.GreetingName(firstName, lastName, email));
        }
    }
}
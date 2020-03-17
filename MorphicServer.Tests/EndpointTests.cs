using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace MorphicServer.Tests
{
    public class EndpointTests: IDisposable
    {

        /// <summary>A test HTTP server</summary>
        protected TestServer Server;

        /// <summary>A client that can make requests to the test server</summary>
        protected HttpClient Client;

        /// <summary>The expected Media Type for JSON requests and responses</summary>
        protected const string JsonMediaType = "application/json";

        /// <summary>The expected character set for JSON requests and responses</sumary>
        protected const string JsonCharacterSet = "utf-8";

        /// <summary>The name of the header for morphic authoriziation tokens</summary>
        protected const string AuthTokenHeaderName = "X-Morphic-Auth-Token";

        /// <summary>A reference to the test database</summary>
        private Database Database;

        /// <summary>Create a test database, test http server, and client connection to the test server</summary>
        public EndpointTests()
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("appsettings.Test.json");
            var builder = new WebHostBuilder();
            builder.UseConfiguration(config.Build());
            builder.UseStartup<Startup>();
            Server = new TestServer(builder);
            Client = Server.CreateClient();
            Database = Server.Services.GetService(typeof(Database)) as Database;
        }

        /// <summary>Delete the test database after every test case so each test can start fresh</summary>
        public void Dispose()
        {
            Database.DeleteDatabase();
        }

        /// <summary>Create a test user and return an auth token</summary>
        protected async Task<UserInfo> CreateTestUser(string firstName = "Test", string lastName = "User")
        {
            ++TestUserCount;
            var content = new Dictionary<string, object>();
            content.Add("key", $"testkey{TestUserCount}");
            content.Add("firstName", firstName);
            content.Add("lastName", lastName);
            var request = new HttpRequestMessage(HttpMethod.Post, "/register/key");
            request.Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, JsonMediaType);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            JsonElement property;
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.True(element.TryGetProperty("token", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            var token = property.GetString();
            Assert.True(element.TryGetProperty("user", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            var user = property;
            Assert.True(user.TryGetProperty("Id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            var id = property.GetString();
            Assert.True(user.TryGetProperty("PreferencesId", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            var preferencesId = property.GetString();
            return new UserInfo()
            {
                Id = id,
                PreferencesId = preferencesId,
                AuthToken = token
            };
        }

        private int TestUserCount = 0;

        protected class UserInfo
        {
            public string Id { get; set; }
            public string PreferencesId { get; set; }
            public string AuthToken { get; set; }
        }

    }
}
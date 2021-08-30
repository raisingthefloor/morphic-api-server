// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Hangfire;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Morphic.Server.Tests
{

    using Db;
    using Users;

    public class EndpointRequestTests: IDisposable
    {

        /// <summary>A test HTTP server</summary>
        protected TestServer Server;

        /// <summary>A client that can make requests to the test server</summary>
        protected HttpClient Client;

        /// <summary>The expected Media Type for JSON requests and responses</summary>
        protected const string JsonMediaType = "application/json";

        /// <summary>The expected character set for JSON requests and responses</summary>
        protected const string JsonCharacterSet = "utf-8";

        /// <summary>A reference to the test database</summary>
        protected Database Database;

        protected MorphicSettings MorphicSettings;

        protected MockBackgroundJobClient JobClient;
        
        /// <summary>Create a test database, test http server, and client connection to the test server</summary>
        public EndpointRequestTests()
        {
            Environment.SetEnvironmentVariable("MORPHIC_ENC_KEY_PRIMARY", "TESTKEY:5E4FA583FDFFEEE0C89E91307A6AD56EDF2DADACDE5163C1485F3FBCC166B995");
            Environment.SetEnvironmentVariable("MORPHIC_HASH_SALT_PRIMARY", "SALT1:361e665ef378ab06031806469b7879bd");
            var config = new ConfigurationBuilder();
            var settingsFile = Environment.GetEnvironmentVariable("APPSETTINGS_FILENAME") ?? "appsettings.Test.json";
            config.AddJsonFile(settingsFile);

            var builder = new WebHostBuilder();
            builder.UseConfiguration(config.Build());
            builder.UseStartup<MockStartup>();
            builder.UseSerilog();
            Server = new TestServer(builder);
            Client = Server.CreateClient();
            Database = Server.Services.GetService(typeof(Database)) as Database;
            MorphicSettings = Server.Services.GetService(typeof(MorphicSettings)) as MorphicSettings;
            Debug.Assert(MorphicSettings != null, nameof(MorphicSettings) + " != null");
            JobClient = Server.Services.GetService((typeof(IBackgroundJobClient))) as MockBackgroundJobClient;
            Debug.Assert(JobClient != null, nameof(JobClient) + " != null");
        }

        /// <summary>Delete the test database after every test case so each test can start fresh</summary>
        public void Dispose()
        {
            Database.DeleteDatabase();
            if (Startup.DotNetRuntimeCollector != null) Startup.DotNetRuntimeCollector.Dispose();
            JobClient.Job = null;
        }

        /// <summary>Create a test user and return an auth token</summary>
        protected async Task<UserInfo> CreateTestUser(string firstName = "Test", string lastName = "User", bool verifiedEmail = false)
        {
            ++TestUserCount;
            var content = new Dictionary<string, object>();
            var username = $"user{TestUserCount}";
            var password = "thisisatestpassword";
            var email = $"user{TestUserCount}" + "@example.com";
            content.Add("username", username);
            content.Add("password", password);
            content.Add("first_name", firstName);
            content.Add("last_name", lastName);
            content.Add("email", email);
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/register/username");
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
            Assert.True(user.TryGetProperty("id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            var id = property.GetString();
            Assert.True(user.TryGetProperty("preferences_id", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.NotEqual("", property.GetString());
            var preferencesId = property.GetString();
            Assert.False(user.TryGetProperty("EmailVerified", out property));

            if (verifiedEmail)
            {
                var dbUser = await Database.Get<User>(id);
                Assert.NotNull(dbUser);
                dbUser.EmailVerified = true;
                await Database.Save(dbUser);
            }

            return new UserInfo()
            {
                Id = id,
                PreferencesId = preferencesId,
                AuthToken = token,
                Username = username,
                Password = password,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };
        }

        private int TestUserCount = 0;

        protected class UserInfo
        {
            public string Id { get; set; }
            public string PreferencesId { get; set; }
            public string AuthToken { get; set; }
            
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public bool EmailVerified { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public async Task<JsonElement> assertJsonError(HttpResponseMessage response, HttpStatusCode code, string error, bool mustContainDetails = true)
        {
            JsonElement property;

            Assert.Equal(code, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.NotNull(response.Content.Headers.ContentType.MediaType);
            Assert.Equal(JsonMediaType, response.Content.Headers.ContentType.MediaType);
            Assert.NotNull(response.Content.Headers.ContentType.CharSet);
            Assert.Equal(JsonCharacterSet, response.Content.Headers.ContentType.CharSet);
            var json = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            Assert.True(element.TryGetProperty("error", out property));
            Assert.Equal(JsonValueKind.String, property.ValueKind);
            Assert.Equal(error, property.GetString());
            if (mustContainDetails)
            {
                Assert.True(element.TryGetProperty("details", out property));
            }
            // don't check value here. Caller can check the details of details.
            return element;
        }

        public void assertMissingRequired(JsonElement error, List<string> missing, bool strict=true)
        {
            JsonElement property;
            Assert.True(error.TryGetProperty("details", out property));
            Assert.Equal(JsonValueKind.Object, property.ValueKind);
            Assert.True(property.TryGetProperty("required", out property));
            Assert.Equal(JsonValueKind.Array, property.ValueKind);
            List<string> propertyArray = new List<string>();
            for (int i = 0; i < property.GetArrayLength(); i++)
            {
                propertyArray.Add(property[i].GetString());
            }
            if (strict)
            {
                Assert.True(Enumerable.SequenceEqual(propertyArray.OrderBy(t => t), missing.OrderBy(t => t)));
            }
            else
            {
                Assert.True(propertyArray.Intersect(missing).Equals(missing));
            }
        }
    }
}

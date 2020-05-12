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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MorphicServer.Tests
{
    public class ValidateEmailEndpointTests : EndpointTests
    {
        [Fact]
        public void TestGetEmailVerificationLinkTemplate()
        {
            // Bad: No headers and nothing in settings
            var settings = new MorphicSettings()
            {
                ServerUrlPrefix = ""
            };
            var requestHeaders = new HeaderDictionary();
            Assert.Throws<ValidateEmailEndpoint.NoServerUrlFoundException>(() => 
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings));

            // bad: settings with bad URL's
            settings.ServerUrlPrefix = "http:///";
            Assert.Throws<ValidateEmailEndpoint.NoServerUrlFoundException>(() => 
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings));

            settings.ServerUrlPrefix = "somehost.com";
            Assert.Throws<ValidateEmailEndpoint.NoServerUrlFoundException>(() => 
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings));

            // GOOD: headers, but no setting: server URL comes from headers
            requestHeaders = new HeaderDictionary
            {
                {"x-forwarded-host", "myhost.example.com"},
                {"x-forwarded-proto", "https"},
                {"x-forwarded-port", "12345"}
            };
            settings.ServerUrlPrefix = "";
            var urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("https://myhost.example.com:12345/v1/verifyEmail/{oneTimeToken}", urlTemplate);

            // Good No headers, but settings has value
            requestHeaders = new HeaderDictionary();
            settings.ServerUrlPrefix = "http://someurl.org:5555";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("http://someurl.org:5555/v1/verifyEmail/{oneTimeToken}", urlTemplate);
            
            // Good: settings take precedence over headers
            requestHeaders = new HeaderDictionary
            {
                {"x-forwarded-host", "myhost.example.com"},
                {"x-forwarded-proto", "https"},
                {"x-forwarded-port", "12345"}
            };
            settings.ServerUrlPrefix = "http://someurl.org:5555";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("http://someurl.org:5555/v1/verifyEmail/{oneTimeToken}", urlTemplate);

            // Good: make sure we trim the trailing /
            requestHeaders = new HeaderDictionary();
            settings.ServerUrlPrefix = "http://someurl.org:5555/";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("http://someurl.org:5555/v1/verifyEmail/{oneTimeToken}", urlTemplate);
            
            // Good. A longer path. Unusual, but let's support it.
            requestHeaders = new HeaderDictionary();
            settings.ServerUrlPrefix = "http://someurl.org:5555/whatever/path/";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("http://someurl.org:5555/whatever/path/v1/verifyEmail/{oneTimeToken}", urlTemplate);

            // No port from headers
            requestHeaders = new HeaderDictionary
            {
                {"x-forwarded-host", "myhost.example.com"},
                {"x-forwarded-proto", "https"},
            };
            settings.ServerUrlPrefix = "";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("https://myhost.example.com/v1/verifyEmail/{oneTimeToken}", urlTemplate);

            // Standard ports from headers
            requestHeaders = new HeaderDictionary
            {
                {"x-forwarded-host", "myhost.example.com"},
                {"x-forwarded-proto", "https"},
                {"x-forwarded-port", "443"}
            };
            settings.ServerUrlPrefix = "";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("https://myhost.example.com/v1/verifyEmail/{oneTimeToken}", urlTemplate);

            requestHeaders = new HeaderDictionary
            {
                {"x-forwarded-host", "myhost.example.com"},
                {"x-forwarded-proto", "http"},
                {"x-forwarded-port", "80"}
            };
            settings.ServerUrlPrefix = "";
            urlTemplate =
                ValidateEmailEndpoint.GetEmailVerificationLinkTemplate(requestHeaders, settings);
            Assert.Equal("http://myhost.example.com/v1/verifyEmail/{oneTimeToken}", urlTemplate);
        }

        [Fact]
        public async Task TestEmailValidationApi()
        {
            // create user
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.Email = "pendingemailuser1@example.com";
            await Database.Save(user);
            Assert.False(user.EmailVerified);
            Assert.Null(await Database.Get<OneTimeToken>(t => t.UserId == user.Id));

            // create OTP
            var oneTimeToken = new OneTimeToken(user.Id);
            await Database.Save(oneTimeToken);

            // check the various fields and hashes
            var token = oneTimeToken.GetUnhashedToken();
            var hashedToken = OneTimeToken.TokenHashedWithDefault(token);
            Assert.NotEqual(token, hashedToken);
            Assert.Equal(hashedToken, oneTimeToken.Id);
            
            // GET, bad token (we're sending the token hash (the ID))
            var path = $"/v1/verifyEmail/{oneTimeToken.Id}";
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotNull(await Database.Get<OneTimeToken>(t => t.UserId == user.Id));
            var sameUser = await Database.Get<User>(user.Id);
            Assert.NotNull(sameUser);
            Assert.False(sameUser.EmailVerified);

            // GET, Good token
            sameUser = await Database.Get<User>(user.Id);
            Assert.NotNull(sameUser);
            Assert.False(sameUser.EmailVerified);
            path = $"/v1/verifyEmail/{token}";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            oneTimeToken = await Database.Get<OneTimeToken>(t => t.UserId == user.Id);
            Assert.NotNull(oneTimeToken);
            Assert.NotNull(oneTimeToken.UsedAt);
            // try the same request again. Will fail
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            var error = await assertJsonError(response, HttpStatusCode.NotFound, "invalid_token");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);


            sameUser = await Database.Get<User>(user.Id);
            Assert.NotNull(sameUser);
            Assert.True(sameUser.EmailVerified);
        }
    }
}
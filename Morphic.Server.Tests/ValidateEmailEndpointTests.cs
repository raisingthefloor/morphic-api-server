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

namespace Morphic.Server.Tests
{

    using Users;
    using Auth;

    public class ValidateEmailEndpointTests : EndpointRequestTests
    {

        [Fact]
        public async Task TestEmailValidationApi()
        {
            // create user
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.Email.PlainText = "pendingemailuser1@example.com";
            await Database.Save(user);
            Assert.False(user.EmailVerified);
            Assert.Null(await Database.Get<OneTimeToken>(t => t.UserId == user.Id));

            // create OTP
            var oneTimeToken = new OneTimeToken(user.Id);
            await Database.Save(oneTimeToken);

            // check the various fields and hashes
            var token = oneTimeToken.GetUnhashedToken();
            Assert.NotEqual(token, oneTimeToken.Token.ToCombinedString());
            Assert.Null(oneTimeToken.UsedAt);
            
            // GET, bad token (we're sending the token hash (the ID))
            var path = $"/v1/users/{user.Id}/verify_email/{oneTimeToken.Id}";
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
            path = $"/v1/users/{user.Id}/verify_email/{token}";
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            oneTimeToken = await Database.Get<OneTimeToken>(t => t.UserId == user.Id);
            Assert.NotNull(oneTimeToken);
            Assert.NotNull(oneTimeToken.UsedAt);
            // try the same request again. Will fail
            request = new HttpRequestMessage(HttpMethod.Get, path);
            response = await Client.SendAsync(request);
            await assertJsonError(response, HttpStatusCode.NotFound, "invalid_token");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);


            sameUser = await Database.Get<User>(user.Id);
            Assert.NotNull(sameUser);
            Assert.True(sameUser.EmailVerified);
        }
    }
}
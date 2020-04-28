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

using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{

    /// <summary>Base class for auth endpoints, containing common logic</summary>
    /// <remarks>
    /// Subclasses are only responsible for providing a request data model and an AuthenticatedUser implementation
    /// </remarks>
    public abstract class AuthEndpoint<T>: Endpoint where T: class
    {

        /// <summary>Parse the request JSON, call AuthenticatedUser, and respond with a token or error</summary>
        public async Task Authenticate()
        {
            var request = await Request.ReadJson<T>();
            var user = await AuthenticatedUser(request);
            if (user == null)
            {
                Log.Logger.Information("USER_MISSING");
                throw new HttpError(HttpStatusCode.BadRequest);
            }

            using (LogContext.PushProperty("UserUid", user.Id))
            {
                var token = new AuthToken(user);
                await Save(token);
                Log.Logger.Debug("NEW_TOKEN for {UserUid}");

                var response = new AuthResponse();
                response.token = token.Id;
                response.user = user;
                await Respond(response);
            }
        }

        public abstract Task<User?> AuthenticatedUser(T request);

    }

    /// <summary>Model for authentication responses</summary>
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? token { get; set; }
        [JsonPropertyName("user")]
        public User? user { get; set; }
    }

    /// <summary>Model for authentication requests using a username</summary>
    public class AuthUsernameRequest
    {
        [JsonPropertyName("username")]
        public string username { get; set; } = "";
        [JsonPropertyName("password")]
        public string password { get; set; } = "";
    }

    /// <summary>Model for authentication requests using a key</summary>
    public class AuthKeyRequest
    {
        [JsonPropertyName("key")]
        public string key { get; set; } = "";
    }

    /// <summary>Authenticate with a username</summary>
    [Path("/v1/auth/username")]
    public class AuthUsernameEndpoint: AuthEndpoint<AuthUsernameRequest>
    {

        public override async Task<User?> AuthenticatedUser(AuthUsernameRequest request)
        {
            var db = Context.GetDatabase();
            return await db.UserForUsername(request.username, request.password);
        }

        [Method]
        public async Task Post()
        {
            await Authenticate();
        }
    }

    /// <summary>Authenticate with a key</summary>
    // Disabling until we have a legitimate use case.  Not removing because we expect
    // to re-enable at some point down the line for something like a USB stick login.
    // [Path("/v1/auth/key")]
    public class AuthKeyEndpoint: AuthEndpoint<AuthKeyRequest>
    {
        public override async Task<User?> AuthenticatedUser(AuthKeyRequest request)
        {
            var db = Context.GetDatabase();
            return await db.UserForKey(request.key);
        }

        [Method]
        public async Task Post()
        {
            await Authenticate();
        }
    }
}
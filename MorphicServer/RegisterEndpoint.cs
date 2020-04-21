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
using MorphicServer.Attributes;
using System.Net;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{

    public class RegisterEndpoint<CredentialType> : Endpoint where CredentialType: Credential
    {

        protected async Task Register(CredentialType credential, RegisterRequest request)
        {
            var prefs = new Preferences();
            prefs.Id = Guid.NewGuid().ToString();
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.FirstName = request.firstName;
            user.LastName = request.lastName;
            user.PreferencesId = prefs.Id;
            credential.UserId = user.Id;
            prefs.UserId = user.Id;
            var token = new AuthToken(user);
            await Save(prefs);
            await Save(user);
            await Save(credential);
            await Save(token);
            var response = new AuthResponse();
            response.token = token.Id;
            response.user = user;
            await Respond(response);

        }

        protected class RegisterRequest
        {
            public string? firstName { get; set; }
            public string? lastName { get; set; }
        }
    }

    /// <summary>Create a new user with a username</summary>
    [Path("/register/username")]
    public class RegisterUsernameEndpoint: RegisterEndpoint<UsernameCredential>
    {
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<RegisterUsernameRequest>();
            if (request.username == "" || request.password == ""){
                 
                Log.Logger.Information("MISSING_USERNAME_PASSWORD");
                throw new HttpError(HttpStatusCode.BadRequest);
            }

            using (LogContext.PushProperty("username", request.username))
            {
                var existing = await Context.GetDatabase().Get<UsernameCredential>(request.username, ActiveSession);
                if (existing != null)
                {
                    Log.Logger.Information("USER_EXISTS({username})");
                    throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponse.ExistingUsername);
                }
                var cred = new UsernameCredential();
                cred.Id = request.username;
                cred.SavePassword(request.password);
                await Register(cred, request);
                Log.Logger.Information("NEW_USER({username})");
            }
        }

        class RegisterUsernameRequest : RegisterRequest
        {
            public string username { get; set; } = "";
            public string password { get; set; } = "";
        }

        class BadRequestResponse
        {
            public string Error { get; set; }

            BadRequestResponse(string error)
            {
                Error = error;
            }

            public static BadRequestResponse ExistingUsername = new BadRequestResponse("ExistingUsername");
        }
    }

    /// <summary>Create a new user with a username</summary>
    [Path("/register/key")]
    public class RegisterKeyEndpoint: RegisterEndpoint<KeyCredential>
    {
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<RegsiterKeyRequest>();
            if (request.key == "")
            {
                throw new HttpError(HttpStatusCode.BadRequest);
            }
            var existing = await Context.GetDatabase().Get<KeyCredential>(request.key, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponse.ExistingKey);
            }
            var cred = new KeyCredential();
            cred.Id = request.key;
            await Register(cred, request);
        }

        class RegsiterKeyRequest : RegisterRequest
        {
            public string key { get; set; } = "";
        }

        class BadRequestResponse
        {
            public string Error { get; set; }

            BadRequestResponse(string error)
            {
                Error = error;
            }

            public static BadRequestResponse ExistingKey = new BadRequestResponse("ExistingKey");
        }
    }
}
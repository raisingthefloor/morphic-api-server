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

using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MorphicServer.Attributes;
using Serilog;

namespace MorphicServer
{
    [Path("/v1/users/{userid}/changePassword")]
    public class ChangePasswordEndpoint : Endpoint
    {
        /// <summary>The user id to use, populated from the request URL</summary>
        [Parameter]
        public string UserId = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            if (authenticatedUser.Id != UserId)
            {
                throw new HttpError(HttpStatusCode.Forbidden);
            }
            
            usernameCredentials = await Load<UsernameCredential>(u => u.UserId == authenticatedUser.Id);
            Log.Logger.Debug("Loaded user credential for {UserId}", authenticatedUser.Id);
        }

        /// <summary>The UsernameCredential data populated by <code>LoadResource()</code></summary>
        private UsernameCredential usernameCredentials = new UsernameCredential();

        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<ChangePasswordRequest>();
            var db = Context.GetDatabase();
            await db.UserForUsernameCredential(usernameCredentials, request.ExistingPassword);
            usernameCredentials.SetPassword(request.NewPassword);
            // TODO Should this invalidate any existing tokens? If yes, should we return a new token here?
            await Save(usernameCredentials);
        }

        /// <summary>Model for change password requests</summary>
        public class ChangePasswordRequest
        {
            [JsonPropertyName("existing_password")]
            public string ExistingPassword { get; set; } = null!;
            [JsonPropertyName("new_password")]
            public string NewPassword { get; set; } = null!;
        }
    }
}
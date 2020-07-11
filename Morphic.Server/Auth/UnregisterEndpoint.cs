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
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Morphic.Server.Auth
{
    using Http;
    using Users;
    using Db;

    public abstract class UnregisterEndpoint : Endpoint
    {
        protected async Task Unregister<TCredential>(User user, TCredential cred) where TCredential : Record
        {
            var db = Context.GetDatabase();
            var deleted = await db.DeleteMany<Preferences>(r => r.UserId == user.Id);
            Log.Logger.Debug($"Deleted {deleted} Preferences");
            deleted = await db.DeleteMany<AuthToken>(r => r.UserId == user.Id);
            Log.Logger.Debug($"Deleted {deleted} AuthTokens");
            deleted = await db.DeleteMany<BadPasswordLockout>(r => r.Id == user.Id);
            Log.Logger.Debug($"Deleted {deleted} AuthTokens");
            await Delete(cred);
            await Delete(user);
        }

        protected UnregisterEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger) : base(contextAccessor, logger)
        {
        }
    }
    
    public class UnregisterUsernameRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";
    }

    public class UnregisterKeyRequest
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "";
    }


    /// <summary>
    /// Endpoint Controller for username unregister requests. Security relies on the username-password
    /// being passed in the request. The reason is two-fold: 1) the user may decide to unregister separately
    /// from using the product (and perhaps not having logge in in a while), so it seems weird to ask them
    /// to log in first, and then unregister (though perhaps that's not so unreasonable), and 2) since this
    /// is a destructive action we don't want someone who hijacks the auth-token to be able to do this.
    ///
    /// TODO We could make this require the auth token, but I would still recommend we make the user enter their password for extra security anyway
    /// </summary>
    [Path("/v1/unregister/username")]
    public class UnregisterUsernameEndpoint : UnregisterEndpoint
    {

        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<UnregisterUsernameRequest>();
            var db = Context.GetDatabase();
            var user = await db.UserForUsername(request.Username, request.Password);
            var cred = await Load<UsernameCredential>(request.Username);
            await Unregister(user, cred);
        }

        public UnregisterUsernameEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger) : base(contextAccessor, logger)
        {
        }
    }
    
    /// <summary>Endpoint Controller for auth-key unregister requests</summary>
    // Disabling until we have a legitimate use case.  Not removing because we expect
    // to re-enable at some point down the line for something like a USB stick login.
    //[Path("/v1/unregister/key")]
    public class UnregisterKeyEndpoint : UnregisterEndpoint
    {
        [Method]
        public async Task Post()
        {
            var request = await Request.ReadJson<UnregisterKeyRequest>();
            var db = Context.GetDatabase();
            var user = await db.UserForKey(request.Key);
            var cred = await Load<UsernameCredential>(request.Key);
            await Unregister(user, cred);
        }

        public UnregisterKeyEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger) : base(contextAccessor, logger)
        {
        }
    }

}

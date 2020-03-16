using System;
using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;

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
            var request = await Request.ReadJson<RegsiterUsernameRequest>();
            var existing = await Context.GetDatabase().Get<UsernameCredential>(request.username, ActiveSession);
            if (existing != null)
            {
                throw new HttpError(HttpStatusCode.BadRequest, BadRequestResponse.ExistingUsername);
            }
            var cred = new UsernameCredential();
            cred.Id = request.username;
            cred.SavePassword(request.password);
            await Register(cred, request);
        }

        class RegsiterUsernameRequest : RegisterRequest
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
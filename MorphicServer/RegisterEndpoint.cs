using System;
using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;

namespace MorphicServer
{

    /// <summary>Create a new user with a username</summary>
    [Path("/register/username")]
    public class RegisterUsernameEndpoint: Endpoint
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
            var prefs = new Preferences();
            prefs.Id = Guid.NewGuid().ToString();
            var user = new User();
            user.Id = Guid.NewGuid().ToString();
            user.FirstName = request.firstName;
            user.LastName = request.lastName;
            user.PreferencesId = prefs.Id;
            prefs.UserId = user.Id;
            var cred = new UsernameCredential();
            cred.Id = request.username;
            cred.UserId = user.Id;
            cred.SavePassword(request.password);
            var token = new AuthToken(user);
            await Save(prefs);
            await Save(user);
            await Save(cred);
            await Save(token);
            var response = new AuthResponse();
            response.token = token.Id;
            response.user = user;
            await Respond(response);
        }

        class RegsiterUsernameRequest
        {
            public string username { get; set; } = "";
            public string password { get; set; } = "";
            public string? firstName { get; set; }
            public string? lastName { get; set; }
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
}
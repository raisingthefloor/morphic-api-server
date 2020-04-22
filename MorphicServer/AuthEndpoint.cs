using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;
using Serilog;
using Serilog.Context;

namespace MorphicServer
{

    /// <summary>Base class for auth endpoints, containing common logic</summary>
    /// <remarks>
    /// Subclasses are only responsible for providing a request data model and an AuthenticatedUser implementation
    /// </remarks>
    public abstract class AuthEndpoint<T>: Endpoint where T: struct
    {

        /// <summary>Parse the request JSON, call AuthenticatedUser, and respond with a token or error</summary>
        public async Task Authenticate()
        {
            var request = await Request.ReadJson<T>();
            if (await AuthenticatedUser(request) is User user)
            {
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
            Log.Logger.Information("USER_MISSING");
            throw new HttpError(HttpStatusCode.BadRequest);
        }

        public abstract Task<User?> AuthenticatedUser(T request);

    }

    /// <summary>Model for authentication responses</summary>
    public struct AuthResponse
    {
        public string? token { get; set; }
        public User? user { get; set; }
    }

    /// <summary>Model for authentication requests using a username</summary>
    public struct AuthUsernameRequest
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    /// <summary>Model for authentication requests using a key</summary>
    public struct AuthKeyRequest
    {
        public string key { get; set; }
    }

    /// <summary>Authenticate with a username</summary>
    [Path("/auth/username")]
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
    [Path("/auth/key")]
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
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
    [Path("/v1/auth/key")]
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
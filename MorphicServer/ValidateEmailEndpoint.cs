using System;
using System.Net;
using System.Threading.Tasks;
using MorphicServer.Attributes;

namespace MorphicServer
{
    [Path("/v1/verifyEmail/{oneTimeToken}")]
    public class ValidateEmailEndpoint : Endpoint
    {
        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string oneTimeToken = "";
        
        /// <summary>The user data populated by <code>LoadResource()</code></summary>
        public User User = null!;
        /// <summary>The limited-use token data populated by <code>LoadResource()</code></summary>
        public OneTimeToken OneTimeToken = null!;

        public override async Task LoadResource()
        {
            OneTimeToken = await Load<OneTimeToken>(t => t.Token == oneTimeToken) ?? throw new HttpError(HttpStatusCode.NotFound);
            User = await Load<User>(OneTimeToken.UserId) ?? throw new HttpError(HttpStatusCode.BadRequest);
        }
        
        /// <summary>Fetch the user</summary>
        [Method]
        public async Task Get()
        {
            User.EmailVerified = true;
            await Save(User);
            await OneTimeToken.Invalidate(Context.GetDatabase());
            // TODO Need to respond with a nicer webpage than ""
        }
    }
}
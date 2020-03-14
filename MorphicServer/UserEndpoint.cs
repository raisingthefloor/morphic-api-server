using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;

namespace MorphicServer
{
    /// <summary>And endpoint representing user preferences</summary>
    [Path("/users/{id}")]
    public class UserEndpoint: Endpoint
    {

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string Id = "";

        /// <summary>Fetch the user from a database</summary>
        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            if (authenticatedUser.Id != Id){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
            User = await Load<User>(Id);
        }

        /// <summary>The user data populated by <code>LoadResource()</code></summary>
        public User User = new User();

        /// <summary>Fetch the user</summary>
        [Method]
        public async Task Get()
        {
            await Respond(User);
        }

        /// <summary>Update the user</summary>
        [Method]
        public async Task Put()
        {
            var updated = await Request.ReadJson<User>();
            User.FirstName = updated.FirstName;
            User.LastName = updated.LastName;
            await Save(User);
        }

        /// <summary>Update the user</summary>
        [Method]
        public async Task Delete()
        {
            await Delete(User);
        }
    }
}
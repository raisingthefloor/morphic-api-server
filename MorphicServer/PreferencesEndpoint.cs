using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;

namespace MorphicServer
{

    /// <summary>And endpoint representing user preferences</summary>
    [Path("/preferences/{id}")]
    public class PreferencesEndpoint: Endpoint
    {

        /// <summary>The lookup id to use, populated from the request URL</summary>
        [Parameter]
        public string Id = "";

        /// <summary>Fetch the preferences from a database</summary>
        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            if (authenticatedUser.PreferencesId != Id){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
            Preferences = await Load<Preferences>(Id);
        }

        /// <summary>The preferences data populated by <code>LoadResource()</code></summary>
        public Preferences Preferences = new Preferences();

        /// <summary>Fetch the user's preferences</summary>
        [Method]
        public async Task Get()
        {
            await Respond(Preferences);
        }

        /// <summary>Update the user's preferences</summary>
        [Method]
        public async Task Put()
        {
            var updated = await Request.ReadJson<Preferences>();
            Preferences.Default = updated.Default;
            await Save(Preferences);
        }

        /// <summary>Update the user's preferences</summary>
        [Method]
        public async Task Delete()
        {
            await Delete(Preferences);
        }
    }
}
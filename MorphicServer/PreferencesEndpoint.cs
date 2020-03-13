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
            // TODO: Will ultimately call async into database
            Preferences = new Preferences(Id);
            await Task.CompletedTask;
        }

        /// <summary>The preferences data populated by <code>LoadResource()</code></summary>
        public Preferences Preferences = new Preferences();

        /// <summary>Fetch the user's preferences</summary>
        [Method]
        public async Task Get()
        {
            await Respond(Preferences);
        }
    }
}
using System.Threading.Tasks;
using MorphicServer.Attributes;
using System;

namespace MorphicServer
{

    [Path("/preferences/")]
    public class PreferencesRootEndpoint: Endpoint
    {
        [Method]
        public async Task Post()
        {
            var preferences = new Preferences();
            preferences.Id = Guid.NewGuid();
            var updated = await Request.ReadJson<Preferences>();
            preferences.UserId = updated.UserId;
            await Save(preferences);
            await Respond(preferences);
        }
    }

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
            Preferences.UserId = updated.UserId;
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
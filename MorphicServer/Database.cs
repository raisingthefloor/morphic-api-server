using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace MorphicServer
{
    /// <summary>Database settings</summary>
    /// <remarks>
    /// Settings are loaded from the appsettings.json file.  See <code>Startup</code> for configuration.
    /// </remarks>
    public class DatabaseSettings
    {
        /// <summary>The database connection URL as a string</summary>
        public string ConnectionString { get; set; } = "";
        
        /// <summary>The database name</summary>
        public string DatabaseName { get; set; } = "";
    }

    /// <summary>A connection to the Morphic database</summary>
    public class Database
    {

        /// <summary>Create a database using the given settings</summary>
        /// <remarks>
        /// Since the database is registered as a service, it is constructed by the service system.
        /// See <code>Startup></code> for service registration.
        /// </remarks>
        public Database(DatabaseSettings settings)
        {
            Client = new MongoClient(settings.ConnectionString);
            Morphic = Client.GetDatabase(settings.DatabaseName);
            CollectionByType[typeof(Preferences)] = Morphic.GetCollection<Preferences>("Preferences");
            CollectionByType[typeof(User)] = Morphic.GetCollection<User>("User");
            CollectionByType[typeof(UsernameCredential)] = Morphic.GetCollection<UsernameCredential>("UsernameCredential");
            CollectionByType[typeof(KeyCredential)] = Morphic.GetCollection<KeyCredential>("KeyCredential");
            CollectionByType[typeof(AuthToken)] = Morphic.GetCollection<AuthToken>("AuthToken");
        }

        /// <summary>The MongoDB client connection</summary>
        private MongoClient Client;

        /// <summary>The Morphic Database</summary>
        private IMongoDatabase Morphic;

        /// <summary>The MongoDB collections within the database</summary>
        private Dictionary<Type, object> CollectionByType = new Dictionary<Type, object>();

        /// <summary>Fetch a record from the database using a string identfier</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<T?> Get<T>(string id) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var result = await collection.FindAsync(record => record.Id == id);
                return result.FirstOrDefault();
            }
            return null;
        }

        /// <summary>Create or update a record in the database</summary>
        /// <remarks>
        /// The destination collection is chosen based on the record's type
        /// </remarks>
        public async Task<bool> Save<T>(T obj) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var options = new ReplaceOptions();
                options.IsUpsert = true;
                var result = await collection.ReplaceOneAsync(record => record.Id == obj.Id, obj, options);
                return result.IsAcknowledged;
            }
            return false;
        }

        /// <summary>Delete a record from the database</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<bool> Delete<T>(T obj) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var result = await collection.DeleteOneAsync(record => record.Id == obj.Id);
                return result.IsAcknowledged;
            }
            return false;
        }

        public void InitializeDatabaseIfNeeded()
        {
            var collection = Morphic.GetCollection<DatabaseInfo>("DatabaseInfo");
            var info = collection.FindSync(info => info.Id == "0").FirstOrDefault();
            if (info == null){
                info = new DatabaseInfo();
                info.Version = 1;
                var authTokens = Morphic.GetCollection<AuthToken>("AuthToken");
                var options = new CreateIndexOptions();
                options.ExpireAfter = TimeSpan.Zero;
                authTokens.Indexes.CreateOne(new CreateIndexModel<AuthToken>(Builders<AuthToken>.IndexKeys.Ascending(t => t.ExpiresAt), options));
                collection.InsertOne(info);
            }
        }

        class DatabaseInfo
        {
            [BsonId]
            public string Id { get; set; } = "0";
            public int Version { get; set; } = 0;
        }

    }

}
// Copyright 2020 Raising the Floor - International
//
// Licensed under the New BSD license. You may not use this file except in
// compliance with this License.
//
// You may obtain a copy of the License at
// https://github.com/GPII/universal/blob/master/LICENSE.txt
//
// The R&D leading to these results received funding from the:
// * Rehabilitation Services Administration, US Dept. of Education under 
//   grant H421A150006 (APCP)
// * National Institute on Disability, Independent Living, and 
//   Rehabilitation Research (NIDILRR)
// * Administration for Independent Living & Dept. of Education under grants 
//   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
// * European Union's Seventh Framework Programme (FP7/2007-2013) grant 
//   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
// * William and Flora Hewlett Foundation
// * Ontario Ministry of Research and Innovation
// * Canadian Foundation for Innovation
// * Adobe Foundation
// * Consumer Electronics Association Foundation

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Clusters;
using Serilog;
using Serilog.Context;

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
            
            using (LogContext.PushProperty("DBSettings", Client.Settings.ToString()))
            using (LogContext.PushProperty("DBName", settings.DatabaseName))
            {
                Log.Logger.Information("Opened DB");
            }

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

    public void DeleteDatabase()
        {
            Client.DropDatabase(Morphic.DatabaseNamespace.DatabaseName);
        }

        public bool IsClusterConnected
        {
            get
            {
                return Client.Cluster.Description.State == ClusterState.Connected;
            }
        }

        /// <summary>The MongoDB collections within the database</summary>
        private Dictionary<Type, object> CollectionByType = new Dictionary<Type, object>();

        /// <summary>Fetch a record from the database using a string identfier</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<T?> Get<T>(string id, Session? session = null) where T: Record
        {
            return await Get<T>(record => record.Id == id, session);
        }

        public async Task<T?> Get<T>(Expression<Func<T, bool>> filter, Session? session = null) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.FindAsync(session.Handle, filter)).FirstOrDefault();
                }
                return (await collection.FindAsync(filter)).FirstOrDefault();
            }
            return null;
        }
        
        /// <summary>Create or update a record in the database</summary>
        /// <remarks>
        /// The destination collection is chosen based on the record's type
        /// </remarks>
        public async Task<bool> Save<T>(T obj, Session? session = null) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var options = new ReplaceOptions();
                options.IsUpsert = true;
                if (session != null)
                {
                    return (await collection.ReplaceOneAsync(session.Handle, record => record.Id == obj.Id, obj, options)).IsAcknowledged;
                }
                return (await collection.ReplaceOneAsync(record => record.Id == obj.Id, obj, options)).IsAcknowledged;
            }
            return false;
        }

        /// <summary>Delete a record from the database</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<bool> Delete<T>(T obj, Session? session = null) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.DeleteOneAsync(session.Handle, record => record.Id == obj.Id)).IsAcknowledged;
                }
                return (await collection.DeleteOneAsync(record => record.Id == obj.Id)).IsAcknowledged;
            }
            return false;
        }

        /// <summary>Run async operations within a transaction, using a lamba to specify the operations</summary>
        /// <remarks>
        /// For most operations that require transactions, a better option is to use the <code>[Method(RunInTransaction=True)]</code>
        /// attribute, which ensures that any operations, incluing <code>LoadResource</code> are run in the transaction.
        /// </remarks>
        public async Task<bool> WithTransaction(Func<Session, Task> operations, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var session = await Client.StartSessionAsync(cancellationToken: cancellationToken))
            {
                var options = new TransactionOptions(
                    readPreference: ReadPreference.Primary,
                    readConcern: ReadConcern.Local,
                    writeConcern: WriteConcern.WMajority
                );

                return await session.WithTransactionAsync(
                    async (session, cancellationToken) =>
                    {
                        await operations(new Session(session));
                        return true;
                    },
                    options,
                    cancellationToken
                );
            }
        }

        /// <summary>Do a one-time database setup or upgrade</summary>
        /// <remarks>
        /// Creates collections and indexes, and keeps a record of initilization in the <code>DatabaseInfo</code> collection.
        /// </remarks>
        public void InitializeDatabaseIfNeeded()
        {
            // FIXME: If multiple servers are spun up at the same time, we could have a situation where each tries to initialize or
            // upgrade the database.  We need some kind of locking system, or this initialization/upgrade code should move to a
            // script that gets run prior to spinning up instances.
            var collection = Morphic.GetCollection<DatabaseInfo>("DatabaseInfo");
            var info = collection.FindSync(info => info.Id == "0").FirstOrDefault();
            if (info == null){
                Morphic.CreateCollection("Preferences");
                
                Morphic.CreateCollection("User");
                var users = Morphic.GetCollection<User>("User");
                users.Indexes.CreateOne(new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Hashed(t => t.EmailHash)));
                
                Morphic.CreateCollection("UsernameCredential");
                Morphic.CreateCollection("KeyCredential");
                Morphic.CreateCollection("AuthToken");
                info = new DatabaseInfo();
                info.Version = 1;
                var authTokens = Morphic.GetCollection<AuthToken>("AuthToken");
                var options = new CreateIndexOptions();
                options.ExpireAfter = TimeSpan.Zero;
                authTokens.Indexes.CreateOne(new CreateIndexModel<AuthToken>(
                    Builders<AuthToken>.IndexKeys.Ascending(t => t.ExpiresAt), options));
                collection.InsertOne(info);
            }
        }

        /// <summary>A record of the database initilization</summary>
        /// <remarks>
        /// The <code>Version</code> field can be used to perform upgrades to the database as needed.
        /// </remarks>
        class DatabaseInfo
        {
            [BsonId]
            public string Id { get; set; } = "0";
            public int Version { get; set; } = 0;
        }

        /// <summary>A database transaction session</summary>
        /// <remarks>
        /// A thin wrapper around the MongoDB session handle to provide some abstraction
        /// </remarks>
        public class Session
        {
            public IClientSessionHandle Handle;

            public Session(IClientSessionHandle handle)
            {
                Handle = handle;
            }
        }

        /// <summary>A MongoDB serializer to store a field as a JSON string</summary>
        /// <remarks>
        /// Useful when the properties contain names or values that MongoDB doesn't allow, such
        /// as dots in property names.
        /// </remarks>
        public class JsonSerializer<T> : SerializerBase<T>
        {

            public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var json = context.Reader.ReadString();
                return JsonSerializer.Deserialize<T>(json);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
            {
                string json = JsonSerializer.Serialize(value);
                context.Writer.WriteString(json);
            }

        }

    }

}
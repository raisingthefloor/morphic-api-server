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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using Serilog;

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
        /// <summary>The MongoDB client connection</summary>
        private readonly MongoClient client;

        /// <summary>The Morphic Database</summary>
        private readonly IMongoDatabase morphic;
        
        /// <summary>Create a database using the given settings</summary>
        /// <remarks>
        /// Since the database is registered as a service, it is constructed by the service system.
        /// See <code>Startup></code> for service registration.
        /// </remarks>
        public Database(DatabaseSettings settings)
        {
            client = new MongoClient(settings.ConnectionString);
            morphic = client.GetDatabase(settings.DatabaseName);

            Log.Logger.Information("Opened DB {Database}: {ConnectionSettings}",
                settings.DatabaseName, client.Settings.ToString());

            CollectionByType[typeof(Preferences)] = morphic.GetCollection<Preferences>("Preferences");
            CollectionByType[typeof(User)] = morphic.GetCollection<User>("User");
            CollectionByType[typeof(UsernameCredential)] =
                morphic.GetCollection<UsernameCredential>("UsernameCredential");
            CollectionByType[typeof(KeyCredential)] = morphic.GetCollection<KeyCredential>("KeyCredential");
            CollectionByType[typeof(AuthToken)] = morphic.GetCollection<AuthToken>("AuthToken");
            CollectionByType[typeof(BadPasswordLockout)] =
                morphic.GetCollection<BadPasswordLockout>("BadPasswordLockout");
            CollectionByType[typeof(OneTimeToken)] = morphic.GetCollection<OneTimeToken>("OneTimeToken");
            CollectionByType[typeof(PendingEmail)] = morphic.GetCollection<PendingEmail>("PendingEmail");
        }

        public void DeleteDatabase()
        {
            client.DropDatabase(morphic.DatabaseNamespace.DatabaseName);
        }

        public bool IsClusterConnected => client.Cluster.Description.State == ClusterState.Connected;

        /// <summary>The MongoDB collections within the database</summary>
        private Dictionary<Type, object> CollectionByType = new Dictionary<Type, object>();

        /// <summary>Fetch a record from the database using a string identfier</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<T?> Get<T>(string id, Session? session = null) where T : Record
        {
            return await Get<T>(record => record.Id == id, session);
        }

        /// <summary>
        /// Fetch a record from the database using a linq filter.
        /// </summary>
        /// <param name="filter">Linq filter</param>
        /// <param name="session">The session</param>
        /// <typeparam name="T">The type of the record/collection</typeparam>
        /// <returns></returns>
        public async Task<T?> Get<T>(Expression<Func<T, bool>> filter, Session? session = null) where T : Record
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
        public async Task<bool> Save<T>(T obj, Session? session = null) where T : Record
        {
            if (obj.Created == default)
            {
                var now = DateTime.UtcNow;
                obj.Created = now;
                obj.Updated = now;
            }
            else
            {
                obj.Updated = DateTime.UtcNow;
            }

            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var options = new ReplaceOptions();
                options.IsUpsert = true;
                if (session != null)
                {
                    return (await collection.ReplaceOneAsync(session.Handle, record => record.Id == obj.Id, obj,
                        options)).IsAcknowledged;
                }

                return (await collection.ReplaceOneAsync(record => record.Id == obj.Id, obj, options)).IsAcknowledged;
            }

            return false;
        }

        /// <summary>Delete a record from the database</summary>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<bool> Delete<T>(T obj, Session? session = null) where T : Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.DeleteOneAsync(session.Handle, record => record.Id == obj.Id))
                        .IsAcknowledged;
                }

                return (await collection.DeleteOneAsync(record => record.Id == obj.Id)).IsAcknowledged;
            }

            return false;
        }

        /// <summary>Run async operations within a transaction, using a lambda to specify the operations</summary>
        /// <remarks>
        /// For most operations that require transactions, a better option is to use the <code>[Method(RunInTransaction=True)]</code>
        /// attribute, which ensures that any operations, including <code>LoadResource</code> are run in the transaction.
        /// </remarks>
        public async Task<bool> WithTransaction(Func<Session, Task> operations,
            CancellationToken cancellationToken = default)
        {
            using (var session = await client.StartSessionAsync(cancellationToken: cancellationToken))
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
        /// Creates collections and indexes.
        /// </remarks>
        public void InitializeDatabase()
        {
            var stopWatch = Stopwatch.StartNew();
            morphic.DropCollection("DatabaseInfo"); // doesn't fail
            
            // TODO: Deal with multi-server database update/upgrade
            // If multiple servers are spun up at the same time, we could have a situation where each
            // tries to initialize or upgrade the database.  We need some kind of locking system, or
            // this initialization/upgrade code should move to a script that gets run prior to spinning
            // up instances.
            CreateCollectionIfNotExists<Preferences>();
            var user = CreateCollectionIfNotExists<User>();
            // IndexExplanation: When registering a new user, we need to make sure no other user has signed
            // up with that email. So we need an index to find it. See RegisterEndpoint.
            CreateOrUpdateIndexOrFail(user,
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Hashed(t => t.EmailHash)));
            CreateCollectionIfNotExists<UsernameCredential>();
            CreateCollectionIfNotExists<KeyCredential>();
            var authToken = CreateCollectionIfNotExists<AuthToken>();
            // IndexExplanation: This collection has documents with expiration, which mongo will automatically remove.
            // the ExpiresAt index is needed to allow Mongo to expire the documents.
            var options = new CreateIndexOptions();
            options.ExpireAfter = TimeSpan.Zero;
            CreateOrUpdateIndexOrFail(authToken,
                new CreateIndexModel<AuthToken>(
                    Builders<AuthToken>.IndexKeys.Ascending(t => t.ExpiresAt), options));
            var badPasswordLockout = CreateCollectionIfNotExists<BadPasswordLockout>();
            // IndexExplanation: This collection has documents with expiration, which mongo will automatically remove.
            // the ExpiresAt index is needed to allow Mongo to expire the documents.
            options = new CreateIndexOptions();
            options.ExpireAfter = TimeSpan.Zero;
            CreateOrUpdateIndexOrFail(badPasswordLockout,
                new CreateIndexModel<BadPasswordLockout>(
                    Builders<BadPasswordLockout>.IndexKeys.Ascending(t => t.ExpiresAt), options));
            
            var oneTimeToken = CreateCollectionIfNotExists<OneTimeToken>();
            // IndexExplanation: This collection has documents with expiration, which mongo will automatically remove.
            // the ExpiresAt index is needed to allow Mongo to expire the documents.
            options.ExpireAfter = TimeSpan.Zero;
            CreateOrUpdateIndexOrFail(oneTimeToken,
                new CreateIndexModel<OneTimeToken>(
                    Builders<OneTimeToken>.IndexKeys.Ascending(t => t.ExpiresAt), options));

            CreateCollectionIfNotExists<PendingEmail>();

            stopWatch.Stop();
            Log.Logger.Information("Database create/update took {TotalElapsedSeconds}secs",
                stopWatch.Elapsed.TotalSeconds);
        }

        private IMongoCollection<T> CreateCollectionIfNotExists<T>()
        {
            var collName = typeof(T).Name;
            try
            {
                morphic.CreateCollection(collName);
                Log.Logger.Debug("Created Collection {Database}.{Collection}", morphic.DatabaseNamespace, collName);
            }
            catch (MongoCommandException e)
            {
                if (e.CodeName != "NamespaceExists")
                    throw;
                Log.Logger.Debug("Collection {Database}.{Collection} existed already (no error)", morphic.DatabaseNamespace,collName);
            }

            return morphic.GetCollection<T>(collName);
        }

        /// <summary>
        /// Wrap MongoCollection.Indexes.CreateOne so that we get some logging and consistent behavior.
        /// 
        /// CreateOne() will do nothing if the index already exists with the same options. It will fail
        /// if the index can not be updated (different options).
        /// 
        /// For our purposes, we will let it throw the exception with the understanding that developers
        /// catch this error during development or test, and deal with it accordingly.
        /// 
        /// Cases we need to manually deal with (or find an automated migration solution):
        /// 1. Need to drop a index that is no longer needed
        /// 2. Need to 'change an index' which is really a 'drop and create' operation. Perhaps we need
        ///    to add such a function later.
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="index"></param>
        /// <typeparam name="T">The Collection Type</typeparam>
        private void CreateOrUpdateIndexOrFail<T>(IMongoCollection<T> collection, CreateIndexModel<T> index)
        {
            var indexName = collection.Indexes.CreateOne(index);
            Log.Logger.Debug(
                "Created/updated index {DBname}.{Collection}:{IndexName}",
                morphic.DatabaseNamespace,
                collection.CollectionNamespace,
                indexName);
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
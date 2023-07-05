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
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using Morphic.Security;

namespace Morphic.Server.Db
{

    using Auth;
    using Users;
    using Community;
    using Billing;

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

        internal readonly ILogger<Database> logger;

        /// <summary>Create a database using the given settings</summary>
        /// <remarks>
        /// Since the database is registered as a service, it is constructed by the service system.
        /// See <code>Startup></code> for service registration.
        /// </remarks>
        public Database(DatabaseSettings settings, ILogger<Database> logger)
        {
            var databaseConnectionString = Database.GetConnectionString();
            if (String.IsNullOrWhiteSpace(databaseConnectionString))
            {
                throw new Exception("Could not retrieve database connection string.");
            }
            var databaseName = Database.GetDatabaseName();
            if (String.IsNullOrWhiteSpace(databaseName))
            {
                 throw new Exception("Could not retrieve database name.");
            }

            this.logger = logger;
            BsonSerializer.RegisterSerializationProvider(new BsonSerializerProvider());
            client = new MongoClient(databaseConnectionString!);
            //client = new MongoClient(settings.ConnectionString);
            morphic = client.GetDatabase(databaseName!);
            //morphic = client.GetDatabase(settings.DatabaseName);

            logger.LogInformation("Opened DB {Database}: {ConnectionSettings}",
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
            CollectionByType[typeof(Community)] = morphic.GetCollection<Community>("Communities");
            CollectionByType[typeof(Member)] = morphic.GetCollection<Member>("CommunityMembers");
            CollectionByType[typeof(Bar)] = morphic.GetCollection<Bar>("CommunityBars");
            CollectionByType[typeof(Invitation)] = morphic.GetCollection<Invitation>("CommunityInvitations");
            CollectionByType[typeof(BillingRecord)] = morphic.GetCollection<BillingRecord>("BillingRecord");
        }

        static private string? GetConnectionString()
        {
            string? keyValue = Morphic.Server.Settings.MorphicAppSecret.GetSecret("api-server", "DATABASESETTINGS__CONNECTIONSTRING");
            return keyValue;
        }

        static private string? GetDatabaseName()
        {
             string? keyValue = Morphic.Server.Settings.MorphicAppSecret.GetSecret("api-server", "DATABASESETTINGS__DATABASENAME");
             return keyValue;
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

        /// <summary>
        /// Get a list of items. Returns an IEnumerable.
        /// </summary>
        /// <param name="filter">Linq filter</param>
        /// <param name="session">The session</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetEnumerable<T>(Expression<Func<T, bool>> filter, Session? session = null) where T : Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.FindAsync(session.Handle, filter)).ToEnumerable();
                }
                return (await collection.FindAsync(filter)).ToEnumerable();
            }
            return new T[] { };
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
            return await Delete<T>(record => record.Id == obj.Id, session);
        }

        public async Task<bool> Delete<T>(Expression<Func<T, bool>> filter, Session? session = null) where T : Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.DeleteOneAsync(session.Handle, filter))
                        .IsAcknowledged;
                }

                return (await collection.DeleteOneAsync(filter)).IsAcknowledged;
            }

            return false;
        }

        /// <summary>Delete one or more records from the database based on an expression</summary>
        /// <returns>Returns the number of records deleted or -1 on error.</returns>
        /// <remarks>
        /// The source collection is chosen based on the record's type
        /// </remarks>
        public async Task<long> DeleteAll<T>(Expression<Func<T, bool>> filter, Session? session = null) where T : Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                if (session != null)
                {
                    return (await collection.DeleteManyAsync(session.Handle, filter)).DeletedCount;
                }

                return (await collection.DeleteManyAsync(filter)).DeletedCount;
            }

            return -1;
        }

        /// <summary>Increment (or decrement) the value of a single field by the given amount</summary>
        /// <returns>Whether the update succeded</returns>
        public async Task<bool> Increment<T, TField>(T obj, Expression<Func<T, TField>> field, TField value) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var builder = new UpdateDefinitionBuilder<T>();
                var update = builder.Inc(field, value);
                var result = await collection.UpdateOneAsync(record => record.Id == obj.Id, update);
                return result.ModifiedCount == 1;
            }
            return false;
        }

        /// <summary>Increment (or decrement) the value of a single field by the given amount</summary>
        /// <returns>Whether the update succeded</returns>
        public async Task<bool> SetField<T, TField>(T obj, Expression<Func<T, TField>> field, TField value) where T: Record
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                var builder = new UpdateDefinitionBuilder<T>();
                var update = builder.Set(field, value);
                var result = await collection.UpdateOneAsync(record => record.Id == obj.Id, update);
                return result.ModifiedCount == 1;
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
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Hashed(u => u.Email.Hash)));
            var usernameCredentials = CreateCollectionIfNotExists<UsernameCredential>();
            // IndexExplanation: When looking up a userCredential, we have the Username in the Username field.
            // See. db.UserForUsername()
            CreateOrUpdateIndexOrFail(usernameCredentials,
                new CreateIndexModel<UsernameCredential>(Builders<UsernameCredential>.IndexKeys.Hashed(uc => uc.Username)));
            // IndexExplanation: When changing a user's password, which lives in the UsernameCredentials collection,
            // we need to look up the UsernameCredentials by that user's ID, so we can change the password.
            // See ChangePasswordEndpoint.
            // IndexUse: This is also needed/used when deleting the user: Need to find the credentials by userId. 
            CreateOrUpdateIndexOrFail(usernameCredentials,
                new CreateIndexModel<UsernameCredential>(
                    Builders<UsernameCredential>.IndexKeys.Hashed(t => t.UserId)));

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
            // IndexExplanation: When looking up a token, we need to search for it. See db.TokenForToken().
            CreateOrUpdateIndexOrFail(oneTimeToken,
                new CreateIndexModel<OneTimeToken>(Builders<OneTimeToken>.IndexKeys.Hashed(t => t.Token)));
            // IndexExplanation: This collection has documents with expiration, which mongo will automatically remove.
            // the ExpiresAt index is needed to allow Mongo to expire the documents.
            options.ExpireAfter = TimeSpan.Zero;
            CreateOrUpdateIndexOrFail(oneTimeToken,
                new CreateIndexModel<OneTimeToken>(
                    Builders<OneTimeToken>.IndexKeys.Ascending(t => t.ExpiresAt), options));

            CreateCollectionIfNotExists<Community>();

            // IndexExplanation: Lookup community members by community id, user id, or both combined
            var communityMembers = CreateCollectionIfNotExists<Member>();
            // https://docs.mongodb.com/manual/reference/method/db.collection.createIndex/#create-an-index-on-a-multiple-fields
            // "A compound index cannot include a hashed index component."
            // So we leave these indexes as regular, unhashed, indexes.
            // CreateOrUpdateIndexOrFail(communityMembers, new CreateIndexModel<Member>(Builders<Member>.IndexKeys.Combine(new IndexKeysDefinition<Member>[]{
            //     Builders<Member>.IndexKeys.Ascending(m => m.CommunityId),
            //     Builders<Member>.IndexKeys.Ascending(m => m.UserId)
            // })));
            CreateOrUpdateIndexOrFail(communityMembers, new CreateIndexModel<Member>(Builders<Member>.IndexKeys.Hashed(m => m.CommunityId)));
            CreateOrUpdateIndexOrFail(communityMembers, new CreateIndexModel<Member>(Builders<Member>.IndexKeys.Hashed(m => m.UserId)));

            // IndexExplanation: Lookup community bars by community id
            var communityBars = CreateCollectionIfNotExists<Bar>();
            CreateOrUpdateIndexOrFail(communityBars, new CreateIndexModel<Bar>(Builders<Bar>.IndexKeys.Hashed(b => b.CommunityId)));

            var communityInvitations = CreateCollectionIfNotExists<Invitation>();

            // IndexExplanation: Invitation expire automatically
            options = new CreateIndexOptions();
            options.ExpireAfter = TimeSpan.Zero;
            CreateOrUpdateIndexOrFail(communityInvitations,
                new CreateIndexModel<Invitation>(
                    Builders<Invitation>.IndexKeys.Ascending(t => t.ExpiresAt), options));


            // IndexExplanation: Stripe webhooks will call with just a subscription id
            var billing = CreateCollectionIfNotExists<BillingRecord>();
            CreateOrUpdateIndexOrFail(billing, new CreateIndexModel<BillingRecord>(Builders<BillingRecord>.IndexKeys.Hashed(b => b.Stripe!.SubscriptionId)));

            // IndexExplanation: Stripe webhooks will call with just a customer id
            CreateOrUpdateIndexOrFail(billing, new CreateIndexModel<BillingRecord>(Builders<BillingRecord>.IndexKeys.Hashed(b => b.Stripe!.CustomerId)));

            stopWatch.Stop();
            logger.LogInformation("Database create/update took {TotalElapsedSeconds}secs",
                stopWatch.Elapsed.TotalSeconds);
        }

        private IMongoCollection<T> CreateCollectionIfNotExists<T>()
        {
            if (CollectionByType[typeof(T)] is IMongoCollection<T> collection)
            {
                try
                {
                    morphic.CreateCollection(collection.CollectionNamespace.CollectionName);
                    logger.LogDebug("Created Collection {Database}.{Collection}", morphic.DatabaseNamespace, collection.CollectionNamespace.CollectionName);
                }
                catch (MongoCommandException e)
                {
                    if (e.CodeName != "NamespaceExists")
                        throw;
                    logger.LogDebug("Collection {Database}.{Collection} existed already", morphic.DatabaseNamespace, collection.CollectionNamespace.CollectionName);
                }
                return collection;
            }
            throw new Exception("No collection registered for type");
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
            logger.LogDebug(
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

        public class BsonSerializerProvider: IBsonSerializationProvider
        {
            public IBsonSerializer? GetSerializer(Type type)
            {
                if (type == typeof(EncryptedField))
                {
                    return new EncryptedField.BsonSerializer();
                }
                if (type == typeof(HashedData))
                {
                    return new HashedData.BsonSerializer();
                }
                if (type == typeof(SearchableHashedString))
                {
                    return new SearchableHashedString.BsonSerializer();
                }
                return null;
            }
        }
    }
}
using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Morphic.Server.Auth;
using Morphic.Server.Db;

namespace Morphic.Server.Users
{
    public class UserCleanupJob
    {
        private readonly Database db;
        private readonly ILogger<UserCleanupJob> logger;
        private readonly MorphicSettings settings;
        
        public UserCleanupJob(
            Database db,
            ILogger<UserCleanupJob> logger,
            MorphicSettings settings)
        {
            this.db = db;
            this.logger = logger;
            this.settings = settings;
        }
        
        [AutomaticRetry(Attempts = 5)]
        public async Task UnregisterUser(string userId)
        {
            var user = await db.Get<User>(userId);
            if (user == null)
            {
                logger.LogInformation("No user found for userId {UserId}", userId);
                // we keep going here to make the job re-entrant. If for some reason it failed earlier,
                // and the user is deleted, we still want to continue cleaning up. Perhaps whatever 
                // error condition existed earlier has resolved itself.
            }
            else
            {
                logger.LogDebug("Deleting user {UserId}", userId);
                await db.Delete(user);
            }

            // delete related entries
            
            var deleted = await db.DeleteMany<Preferences>(r => r.UserId == userId);
            logger.LogDebug("Deleted {deleted} Preferences for user {userId}", deleted, userId);
            deleted = await db.DeleteMany<AuthToken>(r => r.UserId == userId);
            logger.LogDebug("Deleted {deleted} AuthTokens for user {userId}", deleted, userId);
            deleted = await db.DeleteMany<BadPasswordLockout>(r => r.Id == userId);
            logger.LogDebug("Deleted {deleted} BadPasswordLockout for user {userId}", deleted, userId);
            deleted = await db.DeleteMany<UsernameCredential>(u => u.UserId == userId);
            logger.LogDebug("Deleted {deleted} UsernameCredentials for user {userId}", deleted, userId);
            deleted = await db.DeleteMany<KeyCredential>(u => u.UserId == userId);
            logger.LogDebug("Deleted {deleted} KeyCredentials for user {userId}", deleted, userId);
        }

        [DisableConcurrentExecution(60)]
        public async Task DeleteStaleUsers()
        {
            if (settings.StaleUserAfterDays <= 0)
            {
                throw new UserCleanupJobException(
                    $"Can not use settings.StaleUserAfterDays {settings.StaleUserAfterDays}");
            }
            var cutoff = DateTime.UtcNow - new TimeSpan(settings.StaleUserAfterDays, 0, 0, 0);
            using (var cursor = await db.Find<User>(u => u.LastAuth < cutoff))
            {
                int deleted = 0;
                var startedAt = DateTime.UtcNow;
                await cursor.ForEachAsync(user =>
                {
                    logger.LogInformation("Removing stale user {UserId}", user.Id);
                    UnregisterUser(user.Id).Wait();
                    deleted++;
                });
                var elapsed = DateTime.UtcNow - startedAt;
                logger.LogInformation("Deleted {deleted} users in {seconds} seconds", deleted, elapsed.TotalSeconds);
            }
        }

        public class UserCleanupJobException : MorphicServerException
        {
            public UserCleanupJobException(string error) : base(error)
            {
            }
        }

        static public string JobId = "UserCleanupJob.DeleteStaleUsers";

        /// <summary>
        /// Static method to start the recurring job.
        /// </summary>
        public static void StartRecurringJob()
        {
            var cronPeriod = Environment.GetEnvironmentVariable("MORPHIC_CHECK_STALE_USERS_CRON_PERIOD");
            if (string.IsNullOrWhiteSpace(cronPeriod))
            {
                cronPeriod = "weekly";
            }
            switch (cronPeriod.ToLower())
            {
                case "disabled":
                    // don't run the job. Delete if exists.
                    RecurringJob.RemoveIfExists(JobId);
                    return;
                case "daily":
                    cronPeriod = Cron.Daily();
                    break;
                case "weekly":
                    cronPeriod = Cron.Weekly(DayOfWeek.Sunday);
                    break;
                case "monthly":
                    cronPeriod = Cron.Monthly();
                    break;
                case "minutely":
                    // probably only useful for development and or some rare situations!
                    cronPeriod = Cron.Minutely();
                    break;
            }
            RecurringJob.AddOrUpdate<UserCleanupJob>(JobId,
                userCleanup => userCleanup.DeleteStaleUsers(), cronPeriod);
        }
    }
}
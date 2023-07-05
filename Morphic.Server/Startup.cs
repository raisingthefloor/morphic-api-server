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
using System.IO;
using Hangfire;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;

namespace Morphic.Server
{
    using Security;
    using Users;
    using Db;
    using Email;
    using Http;
    using Auth;
    using Billing;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MorphicSettings>(Configuration.GetSection("MorphicSettings"));
            services.AddSingleton<MorphicSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<MorphicSettings>>().Value);
            //
            // TODO: deprecate this method of capturing DatabaseSettings
            services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
            services.AddSingleton<DatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);
            //
            // TODO: deprecate this method of capturing DatabaseSettings
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.AddSingleton<EmailSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailSettings>>().Value);
            //
            // TODO: deprecate this method of capturing KeyStorageSettings
            services.Configure<KeyStorageSettings>(Configuration.GetSection("KeyStorageSettings"));
            services.AddSingleton<KeyStorageSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<KeyStorageSettings>>().Value);
            services.AddSingleton<KeyStorage>(serviceProvider => KeyStorage.CreateShared(serviceProvider.GetRequiredService<KeyStorageSettings>(), serviceProvider.GetRequiredService<ILogger<KeyStorage>>()));
            //
            services.AddSingleton<Plans>(serviceProvider => Plans.FromJson(Path.Join(serviceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "Billing", serviceProvider.GetRequiredService<StripeSettings>().Plans)));
            //
            // TODO: deprecate this method of capturing KeyStorageSettings
            services.Configure<StripeSettings>(Configuration.GetSection("StripeSettings"));
            services.AddSingleton<StripeSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<StripeSettings>>().Value);
            //
            services.AddSingleton<IPaymentProcessor, StripePaymentProcessor>();
            services.AddSingleton<Database>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRecaptcha, Recaptcha>();
            services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
            services.AddRouting();
            services.AddEndpoints();

            var migrationOptions = new MongoMigrationOptions
            {
                Strategy = MongoMigrationStrategy.Migrate,
                BackupStrategy = MongoBackupStrategy.Collections
            };
            string? hangfireConnectionString = Morphic.Server.Settings.MorphicAppSecret.GetSecret("api-server", "HANGFIRESETTINGS__CONNECTIONSTRING") ?? "";
            // var hangfireConnectionString = Configuration.GetSection("HangfireSettings")["ConnectionString"]; // TODO Is there a better way than GetSection[]?
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSerilogLogProvider()
                .UseFilter(new HangfireJobMetrics())
                .UseMongoStorage(hangfireConnectionString,
                    new MongoStorageOptions
                    {
                        MigrationOptions = migrationOptions
                    } )
            );
        }

        // this seems to be needed to dispose of the collector during tests.
        // otherwise we don't care about disposing them
        public static IDisposable? DotNetRuntimeCollector;
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Database database, KeyStorage keyStorage, ILogger<Startup> logger)
        {
            logger.LogInformation("Startup.Configure called");
            keyStorage.LoadKeysFromEnvIfNeeded();
            if (DotNetRuntimeCollector == null && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DISABLE_EXTENDED_METRICS")))
            {
                // https://github.com/djluck/prometheus-net.DotNetRuntime
                DotNetRuntimeCollector = DotNetRuntimeStatsBuilder.Customize()
                    // Only 1 in 10 contention events will be sampled 
                    .WithContentionStats(sampleRate: SampleEvery.TenEvents)
                    // Only 1 in 100 JIT events will be sampled
                    .WithJitStats(sampleRate: SampleEvery.HundredEvents)
                    // Every event will be sampled (disables sampling)
                    .WithThreadPoolSchedulingStats(sampleRate: SampleEvery.OneEvent)
                    .StartCollecting();
            }

            database.InitializeDatabase();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseSerilogRequestLogging();
            //app.UseHttpMetrics(); // doesn't work. Probably because we have our own mapping, and something is missing
            app.UseEndpoints(Endpoint.All);
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
            });
            app.UseHangfireServer();
            app.UseHangfireDashboard();

            UserCleanupJob.StartRecurringJob();
        }
    }
}

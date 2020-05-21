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
using Hangfire;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Mongo;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;

namespace MorphicServer
{
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
            services.Configure<DatabaseSettings>(Configuration.GetSection("DatabaseSettings"));
            services.AddSingleton<DatabaseSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value);
            services.Configure<EmailSettings>(Configuration.GetSection("EmailSettings"));
            services.AddSingleton<EmailSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<EmailSettings>>().Value);
            services.AddSingleton<Database>();
            services.AddRouting();

            var migrationOptions = new MongoMigrationOptions
            {
                Strategy = MongoMigrationStrategy.Migrate,
                BackupStrategy = MongoBackupStrategy.Collections
            };
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSerilogLogProvider()
                .UseFilter(new LogFailureAttribute())
                .UseMongoStorage(Configuration.GetSection("HangfireSettings")["ConnectionString"], // TODO Is there a better way than GetSection[]?
                    new MongoStorageOptions
                    {
                        MigrationOptions = migrationOptions
                    } )
            );

            // load the keys. Fails if they aren't present.
            KeyStorage.LoadKeysFromEnvIfNeeded();
        }

        // this seems to be needed to dispose of the collector during tests.
        // otherwise we don't care about disposing them
        public static IDisposable? DotNetRuntimeCollector;
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Database database)
        {
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
        }
    }
    
    public class MorphicSettings
    {
        /// <summary>The Server URL prefix. Used to generate URLs for various purposes.</summary>
        public string ServerUrlPrefix { get; set; } = "";
    }

    public class HangfireSettings
    {
        public string ConnectionString { get; set; } = "";
    }
    
    public class LogFailureAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var failedState = context.NewState as FailedState;
            if (failedState != null)
            {
                Logger.ErrorException(
                    String.Format("Background job #{0} was failed with an exception.", context.BackgroundJob.Id),
                    failedState.Exception);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}

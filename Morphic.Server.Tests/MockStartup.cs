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

using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Morphic.Security;
using Serilog;

namespace Morphic.Server.Tests
{
    public class MockStartup
    {
        public MockStartup(IConfiguration configuration)
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
            services.Configure<KeyStorageSettings>(Configuration.GetSection("KeyStorageSettings"));
            services.AddSingleton<KeyStorageSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<KeyStorageSettings>>().Value);
            services.AddSingleton<KeyStorage>(serviceProvider => KeyStorage.CreateShared(serviceProvider.GetRequiredService<KeyStorageSettings>(), serviceProvider.GetRequiredService<ILogger<KeyStorage>>()));
            services.AddSingleton<Database>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRecaptcha, MockRecaptcha>();
            services.AddSingleton<IBackgroundJobClient, MockBackgroundJobClient>();

            services.AddRouting();
            services.AddEndpoints();
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Database database, KeyStorage keyStorage)
        {
            keyStorage.LoadKeysFromEnvIfNeeded();
            database.InitializeDatabase();
            app.UseRouting();
            app.UseSerilogRequestLogging();
            //app.UseHttpMetrics(); // doesn't work. Probably because we have our own mapping, and something is missing
            app.UseEndpoints(Endpoint.All);
        }
    }
}

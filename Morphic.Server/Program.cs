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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace Morphic.Server
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var hostBuilder = CreateHostBuilder(args);
            var host = hostBuilder.Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = new HostBuilder();
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureHostConfiguration(ConfigureHost);
            builder.ConfigureAppConfiguration(ConfigureApp);
            builder.UseSerilog(ConfigureSerilog);
            builder.ConfigureWebHostDefaults(ConfigureWebHost);
            return builder;
        }

        public static void ConfigureHost(IConfigurationBuilder configuration)
        {
            configuration.AddEnvironmentVariables();
        }

        public static void ConfigureApp(HostBuilderContext context, IConfigurationBuilder configuration)
        {
            var envname = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            configuration.SetBasePath(context.HostingEnvironment.ContentRootPath);
            configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            configuration.AddJsonFile($"appsettings.{envname}.json", optional: true, reloadOnChange: false);
            if (envname == "Development")
            {
                configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
                configuration.AddUserSecrets<Program>();
            }
            configuration.AddEnvironmentVariables();
        }

        public static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration serilog)
        {
            serilog.ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new SerilogMetrics())
                .WriteTo.Console(new JsonFormatter());
        }

        public static void ConfigureWebHost(IWebHostBuilder webHost)
        {
            webHost.UseKestrel(opt => opt.AddServerHeader = false);
            webHost.UseStartup<Startup>();
        }
    }
}

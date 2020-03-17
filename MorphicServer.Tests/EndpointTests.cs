using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace MorphicServer.Tests
{
    public class EndpointTests: IDisposable
    {

        protected TestServer Server;
        protected HttpClient Client;
        protected const string JsonMediaType = "application/json";
        protected const string JsonCharacterSet = "utf-8";
        protected const string AuthTokenHeaderName = "X-Morphic-Auth-Token";
        private Database Database;

        public EndpointTests()
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("appsettings.Test.json");
            var builder = new WebHostBuilder();
            builder.UseConfiguration(config.Build());
            builder.UseStartup<Startup>();
            Server = new TestServer(builder);
            Client = Server.CreateClient();
            Database = Server.Services.GetService(typeof(Database)) as Database;
        }

        public void Dispose()
        {
            Database.DeleteDatabase();
        }

    }
}
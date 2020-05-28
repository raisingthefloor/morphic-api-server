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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MorphicServer.Attributes;

namespace MorphicServer.Tests
{

    public class EndpointTests
    {

        private class MockHttpRequest: HttpRequest
        {

            public MockHttpRequest(HttpContext context)
            {
                HttpContext = context;
            }

            public override HttpContext HttpContext { get; }
            public override string Protocol { get; set; } = "HTTP/1.1";
            public override string Scheme { get; set; } = "http";
            public override HostString Host { get; set; } = new HostString("test.host");
            public override PathString PathBase { get; set; } = "";
            public override PathString Path { get; set; } = "";
            public override string Method { get; set; } = "GET";
            public override QueryString QueryString { get; set; }
            public override IRequestCookieCollection Cookies { get; set; }
            public override bool IsHttps { get; set; } = false;
            public override bool HasFormContentType { get; } = false;
            public override Stream Body { get; set; }
            public override IQueryCollection Query { get; set; }
            public override IFormCollection Form { get; set; }
            public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
            public override string ContentType { get; set; } = "";
            public override long? ContentLength { get; set; }

            public override Task<IFormCollection>  ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

        }

        private class MockHttpResponse: HttpResponse
        {

            public MockHttpResponse(HttpContext context)
            {
                HttpContext = context;
            }

            public override HttpContext HttpContext { get; }
            private bool hasStarted = false;
            public override bool HasStarted { get { return hasStarted; } }
            public override Stream Body { get; set; } = new MemoryStream();
            public override int StatusCode { get; set; } = 0;
            public override IResponseCookies Cookies { get; }
            public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
            public override string ContentType { get; set; }
            public override long? ContentLength { get; set; }

            public override void OnCompleted(Func<object, Task> callback, object state)
            {
            }

            private Func<object, Task> startingCallback = null;
            private object startingCallbackState = null;

            public override void OnStarting(Func<object, Task> callback, object state)
            {
                startingCallback = callback;
                startingCallbackState = state;
                hasStarted = true;
            }

            public override void Redirect(string location, bool permanent)
            {
            }

            public override async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                if (startingCallback != null){
                    await startingCallback.Invoke(startingCallbackState);
                }
            }

            public override Task CompleteAsync()
            {
                return Task.CompletedTask;
            }
        }

        private class MockHttpContext: HttpContext
        {

            public MockHttpContext(IServiceProvider serviceProvider)
            {
                RequestServices = serviceProvider;
                Request = new MockHttpRequest(this);
                Response = new MockHttpResponse(this);
            }

            public override HttpRequest Request { get; }
            public override HttpResponse Response { get; }
            public override IServiceProvider RequestServices { get; set; }
            public override CancellationToken RequestAborted { get; set; } = new CancellationToken();
            public override ISession Session { get; set; }
            public override string TraceIdentifier { get; set; } = "TESTING";
            public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
            public override ClaimsPrincipal User { get; set; } = new ClaimsPrincipal();
            public override IFeatureCollection Features { get; } = new FeatureCollection();
            public override ConnectionInfo Connection { get; }
            public override WebSocketManager WebSockets { get; }

            public override void Abort()
            {

            }
        }

        private class MockContextAccessor: IHttpContextAccessor
        {

            public MockContextAccessor(HttpContext context)
            {
                HttpContext = context;
            }

            public HttpContext HttpContext { get; set; }
        }

        private class MockEndpoint: Endpoint
        {
            public MockEndpoint(IHttpContextAccessor contextAccessor, ILogger<MockEndpoint> logger): base(contextAccessor, logger)
            {
            }
        }

        [Fact]
        public void TestServerUri()
        {
            // No setting, no request headers
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<MockEndpoint>();
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var endpoint = provider.GetRequiredService<MockEndpoint>();
            var uri = endpoint.ServerUri;
            Assert.Equal("", uri.ToString());

            // No setting, with request headers
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<MockEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Headers.Add("x-forwarded-host", "myhost.example.com");
            context.Request.Headers.Add("x-forwarded-proto", "https");
            context.Request.Headers.Add("x-forwarded-port", "12345");
            endpoint = provider.GetRequiredService<MockEndpoint>();
            uri = endpoint.ServerUri;
            Assert.Equal("https://myhost.example.com:12345/", uri.ToString());

            // With setting, with request headers
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings(){
                ServerUrlPrefix="https://settings.host"
            });
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<MockEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Headers.Add("x-forwarded-host", "myhost.example.com");
            context.Request.Headers.Add("x-forwarded-proto", "https");
            context.Request.Headers.Add("x-forwarded-port", "12345");
            endpoint = provider.GetRequiredService<MockEndpoint>();
            uri = endpoint.ServerUri;
            Assert.Equal("https://settings.host/", uri.ToString());

            // With setting, no request headers
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings(){
                ServerUrlPrefix="https://settings.host"
            });
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<MockEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            endpoint = provider.GetRequiredService<MockEndpoint>();
            uri = endpoint.ServerUri;
            Assert.Equal("https://settings.host/", uri.ToString());
        }

        [Attributes.Path("cors")]
        [AllowedOrigin("https://other.origin")]
        private class CorsEndpoint: Endpoint
        {

            public CorsEndpoint(IHttpContextAccessor contextAccessor, ILogger<CorsEndpoint> logger): base(contextAccessor, logger)
            {
            }

            [Method]
            public async Task Get()
            {
                await Respond(new Dictionary<string, object>()
                {
                    {"test", "hi"}
                });
            }
        }

        [Attributes.Path("cors2")]
        [AllowedOrigin("*")]
        private class CorsWildcardEndpoint: Endpoint
        {

            public CorsWildcardEndpoint(IHttpContextAccessor contextAccessor, ILogger<CorsEndpoint> logger): base(contextAccessor, logger)
            {
            }

            [Method]
            public async Task Get()
            {
                await Respond(new Dictionary<string, object>()
                {
                    {"test", "hi"}
                });
            }
        }

        [Fact]
        public async Task TestCrossOrigin()
        {
            // Not sending Origin
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsEndpoint>();
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            await Endpoint.Run<CorsEndpoint>(context);
            var allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            var allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            var allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            var vary = context.Response.Headers["Vary"];
            Assert.Empty(allowedOrigin);
            Assert.Empty(allowedMethods);
            Assert.Empty(allowedHeaders);
            Assert.Empty(vary);

            // Sending disallowed origin
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            context.Request.Headers.Add("Origin", "https://wrong.origin");
            await Endpoint.Run<CorsEndpoint>(context);
            allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            vary = context.Response.Headers["Vary"];
            Assert.Empty(allowedOrigin);
            Assert.Empty(allowedMethods);
            Assert.Empty(allowedHeaders);
            Assert.Empty(vary);

            // Sending allowed origin
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            context.Request.Headers.Add("Origin", "https://other.origin");
            await Endpoint.Run<CorsEndpoint>(context);
            allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            vary = context.Response.Headers["Vary"];
            Assert.Single(allowedOrigin);
            Assert.Equal("https://other.origin", allowedOrigin[0]);
            Assert.Empty(allowedMethods);
            Assert.Empty(allowedHeaders);
            Assert.Single(vary);
            Assert.Equal("Origin", vary[0]);

            // Sending allowed origin with method
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            context.Request.Headers.Add("Origin", "https://other.origin");
            context.Request.Headers.Add("Access-Control-Request-Method", "GET");
            await Endpoint.Run<CorsEndpoint>(context);
            allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            vary = context.Response.Headers["Vary"];
            Assert.Single(allowedOrigin);
            Assert.Equal("https://other.origin", allowedOrigin[0]);
            Assert.Single(allowedMethods);
            Assert.Equal("*", allowedMethods[0]);
            Assert.Empty(allowedHeaders);
            Assert.Single(vary);
            Assert.Equal("Origin", vary[0]);

            // Sending allowed origin with method and headers
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            context.Request.Headers.Add("Origin", "https://other.origin");
            context.Request.Headers.Add("Access-Control-Request-Method", "GET");
            context.Request.Headers.Add("Access-Control-Request-Headers", "Content-Type");
            await Endpoint.Run<CorsEndpoint>(context);
            allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            vary = context.Response.Headers["Vary"];
            Assert.Single(allowedOrigin);
            Assert.Equal("https://other.origin", allowedOrigin[0]);
            Assert.Single(allowedMethods);
            Assert.Equal("*", allowedMethods[0]);
            Assert.Single(allowedHeaders);
            Assert.Equal("Content-Type, Authorization", allowedHeaders[0]);
            Assert.Single(vary);
            Assert.Equal("Origin", vary[0]);

            // Adding wildcard
            services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MorphicSettings>(new MorphicSettings());
            services.AddSingleton<IHttpContextAccessor>(provider => new MockContextAccessor(new MockHttpContext(provider)));
            services.AddTransient<CorsWildcardEndpoint>();
            provider = services.BuildServiceProvider();
            context = provider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            context.Request.Path = "/cors";
            context.Request.Method = "GET";
            context.Request.Headers.Add("Origin", "https://wildcard.origin");
            context.Request.Headers.Add("Access-Control-Request-Method", "GET");
            context.Request.Headers.Add("Access-Control-Request-Headers", "Content-Type");
            await Endpoint.Run<CorsWildcardEndpoint>(context);
            allowedOrigin = context.Response.Headers["Access-Control-Allow-Origin"];
            allowedMethods = context.Response.Headers["Access-Control-Allow-Methods"];
            allowedHeaders = context.Response.Headers["Access-Control-Allow-Headers"];
            vary = context.Response.Headers["Vary"];
            Assert.Single(allowedOrigin);
            Assert.Equal("https://wildcard.origin", allowedOrigin[0]);
            Assert.Single(allowedMethods);
            Assert.Equal("*", allowedMethods[0]);
            Assert.Single(allowedHeaders);
            Assert.Equal("Content-Type, Authorization", allowedHeaders[0]);
            Assert.Empty(vary);

        }

    }

}
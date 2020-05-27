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

namespace MorphicServer.Tests
{

    public class EndpointTests
    {

        private class MockHttpRequest: HttpRequest
        {

            public MockHttpRequest(HttpContext context)
            {
                HttpContext = context;
                Headers = new HeaderDictionary();
            }

            public override HttpContext HttpContext { get; }
            public override string Protocol { get; set; }
            public override string Scheme { get; set; }
            public override HostString Host { get; set; }
            public override PathString PathBase { get; set; }
            public override PathString Path { get; set; }
            public override string Method { get; set; }
            public override QueryString QueryString { get; set; }
            public override IRequestCookieCollection Cookies { get; set; }
            public override bool IsHttps { get; set; }
            public override bool HasFormContentType { get; }
            public override Stream Body { get; set; }
            public override IQueryCollection Query { get; set; }
            public override IFormCollection Form { get; set; }
            public override IHeaderDictionary Headers { get; }
            public override string ContentType { get; set; }
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
            public override bool HasStarted { get; }
            public override Stream Body { get; set; }
            public override int StatusCode { get; set; }
            public override IResponseCookies Cookies { get; }
            public override IHeaderDictionary Headers { get; }
            public override string ContentType { get; set; }
            public override long? ContentLength { get; set; }

            public override void OnCompleted(Func<object, Task> callback, object state)
            {
            }

            public override void OnStarting(Func<object, Task> callback, object state)
            {
            }

            public override void Redirect(string location, bool permanent)
            {
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
            public override CancellationToken RequestAborted { get; set; }
            public override ISession Session { get; set; }
            public override string TraceIdentifier { get; set; }
            public override IDictionary<object, object> Items { get; set; }
            public override ClaimsPrincipal User { get; set; }
            public override IFeatureCollection Features { get; }
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

    }

}
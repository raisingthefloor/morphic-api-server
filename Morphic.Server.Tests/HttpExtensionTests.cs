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
using Xunit;
using Microsoft.AspNetCore.Http;

namespace Morphic.Server.Tests
{

    using Http;
    
    public class HttpExtensionTests
    {

        private class MockHttpRequest: HttpRequest
        {

            public MockHttpRequest()
            {
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

        [Fact]
        public void TestHttpRequestGetServerUri()
        {
            // Bad: No headers and nothing in settings
            var settings = new MorphicSettings()
            {
                ServerUrlPrefix = ""
            };
            var request = new MockHttpRequest();
            request.Headers.Add("foo1", "something1");
            request.Headers.Add("foo2", "something2");
            var uri = request.GetServerUri();
            Assert.Null(uri);

            // GOOD: headers, but no setting: server URL comes from headers
            request = new MockHttpRequest();
            request.Headers.Add("x-forwarded-host", "myhost.example.com");
            request.Headers.Add("x-forwarded-proto", "https");
            request.Headers.Add("x-forwarded-port", "12345");
            uri = request.GetServerUri();
            Assert.Equal("https://myhost.example.com:12345/", uri.ToString());

            // No port from headers
            request = new MockHttpRequest();
            request.Headers.Add("x-forwarded-host", "myhost.example.com");
            request.Headers.Add("x-forwarded-proto", "https");
            uri = request.GetServerUri();
            Assert.Equal("https://myhost.example.com/", uri.ToString());

            // Standard ports from headers
            request = new MockHttpRequest();
            request.Headers.Add("x-forwarded-host", "myhost.example.com");
            request.Headers.Add("x-forwarded-proto", "https");
            request.Headers.Add("x-forwarded-port", "443");
            uri = request.GetServerUri();
            Assert.Equal("https://myhost.example.com/", uri.ToString());

            request = new MockHttpRequest();
            request.Headers.Add("x-forwarded-host", "myhost.example.com");
            request.Headers.Add("x-forwarded-proto", "http");
            request.Headers.Add("x-forwarded-port", "80");
            uri = request.GetServerUri();
            Assert.Equal("http://myhost.example.com/", uri.ToString());
        }

    }

}
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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using System.Text.Json;
using System.Text;
using Morphic.Server.Community;
using Morphic.Server.Http;

namespace Morphic.Server.Tests
{
    public class ValidateLinkEndpointTests
    {
        /// <summary>Urls that will be tested on live hosts.</summary>
        public static string[] LiveGoodLinks => new[]
        {
            "http://github.com", "https://github.com",
            "http://httpstat.us/200", "http://httpstat.us/201"
        };

        /// <summary>Urls that will be tested on live hosts, but should fail.</summary>
        public static string[] LiveBadLinks => new[]
        {
            "http://httpstat.us/404", "https://httpstat.us/500",
            "http://example.invalid/"
        };

        /// <summary>Non-live valid urls that should be connected to.</summary>
        public static string[] GoodLinks => new[]
        {
            "http://httpstat.us/404", // would fail if live
            "http://example.invalid/",
            "http://example.com/",
            "http://example.com/aaa",
            "http://example.com/aaa?bbb",
        };

        /// <summary>Urls that should not be connected to.</summary>
        public static string[] BadLinks => new[]
        {
            "", "http://", "http:///", "file://aa/bb", "file:///example.com/bb",
            "http:///example.com",
            "http://example.com:34",
            "http://a:b@example.com",
            "http://a@example.com",
            "http://a:b@example.com:11",
            "ftp://example.com",
            "example.com",
            "http://10.11.12.13"
        };

        [Theory]
        [MemberData(nameof(GetLiveLinks))]
        public async Task TestLiveLink(string url, bool expectGood)
        {
            bool isGood;
            try
            {
                await ValidateLinkEndpoint.CheckLink(url, "0.0.0.0");
                isGood = true;
            }
            catch (HttpError)
            {
                isGood = false;
            }

            Assert.Equal(expectGood, isGood);
        }

        [Theory]
        [MemberData(nameof(GetTestLinks))]
        public async Task TestLink(string url, bool expectRequest)
        {
            bool isGood;
            bool gotRequest = false;
            try
            {
                await ValidateLinkEndpoint.CheckLink(url, "0.0.0.0", connectionUrl =>
                {
                    // check the correct URL is being requested
                    Assert.Equal(new Uri(url).ToString(), connectionUrl.ToString());

                    gotRequest = true;
                });
                isGood = true;
            }
            catch (HttpError)
            {
                isGood = false;
            }

            Assert.Equal(expectRequest, gotRequest);
            Assert.Equal(expectRequest, isGood);
        }

        public static IEnumerable<object[]> GetLiveLinks()
        {
            return LiveGoodLinks.Select(link => new object[] {link, true})
                .Concat(LiveBadLinks.Select(link => new object[] {link, false}));
        }

        public static IEnumerable<object[]> GetTestLinks()
        {
            return GoodLinks.Select(link => new object[] {link, true})
                .Concat(BadLinks.Select(link => new object[] {link, false}));
        }

    }
}
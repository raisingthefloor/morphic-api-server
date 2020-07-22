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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Morphic.Server.Community
{

    using Http;
    using Users;
    using Json;

    [Path("/v1/communities/{id}/bars")]
    public class BarsEndpoint: Endpoint
    {

        public BarsEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            User = await RequireUser();
            Community = await Load<Community>(Id);
            Member = await Load<Member>(m => m.UserId == User.Id && m.CommunityId == Community.Id && m.State == MemberState.Active);
            if (Member.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        private Member Member = null!;
        private Community Community = null!;
        private User User = null!;

        [Method]
        public async Task Get()
        {
            var db = Context.GetDatabase();
            var bars = await db.GetEnumerable<Bar>(b => b.CommunityId == Community.Id);
            var collection = new BarCollection();
            foreach (var bar in bars){
                collection.Bars.Add(new BarCollectionItem()
                {
                    Id = bar.Id,
                    Name = bar.Name,
                    IsShared = bar.IsShared
                });
            }
            await Respond(collection);
        }

        private class BarCollection{
            [JsonPropertyName("bars")]
            public List<BarCollectionItem> Bars { get; set; } = new List<BarCollectionItem>();
        }

        private class BarCollectionItem{
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("is_shared")]
            public bool IsShared { get; set; }
        }

        [Method]
        public async Task Post()
        {
            var db = Context.GetDatabase();
            var input = await Request.ReadJson<BarPostRequest>();

            var bar = new Bar()
            {
                Id = Guid.NewGuid().ToString(),
                CommunityId = Community.Id,
                Name = input.Name,
                IsShared = input.IsShared,
                Items = input.Items,
                CreatedAt = DateTime.Now
            };
            await db.Save(bar);
            await Respond(new BarPostResponse()
            {
                Bar = bar
            });
        }

        class BarPostRequest
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;

            [JsonPropertyName("is_shared")]
            [JsonRequired]
            public bool IsShared { get; set; }

            [JsonPropertyName("items")]
            public BarItem[] Items { get; set; } = null!;
        }

        class BarPostResponse
        {
            [JsonPropertyName("bar")]
            public Bar Bar { get; set; } = null!;
        }
    }

}
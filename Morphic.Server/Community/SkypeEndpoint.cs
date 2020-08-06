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
using System.Net.Http;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace Morphic.Server.Community{

    using Http;

    [Path("/v1/communities/{id}/skype/meetings")]
    public class SkypeEndpoint: Endpoint
    {

        public SkypeEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            var authenticatedUser = await RequireUser();
            Community = await Load<Community>(Id);
            var member = await Load<Member>(m => m.CommunityId == Community.Id && m.UserId == authenticatedUser.Id && m.State == MemberState.Active);
            if (member.Role != MemberRole.Manager){
                throw new HttpError(HttpStatusCode.Forbidden);
            }
        }

        public Community Community = null!;

        [Method]
        public async Task Post()
        {
            var input = await Request.ReadJson<SkypeRequest>();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.join.skype.com/v1/meetnow/createjoinlinkguest");
            var json = JsonSerializer.Serialize(input);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK){
                throw new HttpError(response.StatusCode);
            }
            var jsonStream = await response.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<SkypeResponse>(jsonStream);
            await Respond(obj);
        }

        class SkypeRequest
        {
            public string Title { get; set; } = null!;
        }

        class SkypeResponse
        {
            [JsonPropertyName("joinLink")]
            public string JoinLink { get; set; } = null!;
        }
    }

}
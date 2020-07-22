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
using Hangfire;

namespace Morphic.Server.Auth
{

    using Http;
    using Users;
    using Community;
    using Json;

    [Path("/v1/invitations/{id}")]
    public class InvitationEndpoint: Endpoint
    {

        public InvitationEndpoint(IHttpContextAccessor contextAccessor, ILogger<Endpoint> logger): base(contextAccessor, logger)
        {
        }

        [Parameter]
        public string Id = "";

        public override async Task LoadResource()
        {
            Invitation = await Load<Invitation>(Id);
            Community = await Load<Community>(Invitation.CommunityId);
            Member = await Load<Member>(Invitation.MemberId);
        }

        private Member Member = null!;
        private Community Community = null!;
        private Invitation Invitation = null!;

        [Method]
        public async Task Get()
        {
            await Respond(new InvitationInfo()
            {
                Community = new CommunityInfo()
                {
                    Id = Community.Id,
                    Name = Community.Name
                },
                Email = Invitation.Email.PlainText!,
                FirstName = Member.FirstName.PlainText,
                LastName = Member.LastName.PlainText
            });
        }

        class InvitationInfo
        {
            [JsonPropertyName("community")]
            public CommunityInfo Community { get; set; } = null!;
            
            [JsonPropertyName("email")]
            public string Email { get; set; } = null!;
            
            [JsonPropertyName("first_name")]
            public string? FirstName { get; set; }

            [JsonPropertyName("last_name")]
            public string? LastName { get; set; }
        }

        class CommunityInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("name")]
            public string Name { get; set; } = null!;
        }

    }

}
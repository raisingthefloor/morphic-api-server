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
using System.Threading.Tasks;
using MorphicServer.Attributes;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MorphicServer
{
    /// <summary>And endpoint to check server readiness</summary>
    [Path("/ready")]
    [OmitMetrics]
    public class ReadyEndpoint : Endpoint
    {

        [Method]
        public async Task Get()
        {
            if (readyResponse == null)
            {
                throw new HttpError(HttpStatusCode.BadRequest);
            }
            Response.ContentType = "application/json";
            await Response.WriteAsync(readyResponse, Context.RequestAborted);
        }

        private static string? readyResponse;
        
        public class PeriodicReadyCheckService : BackgroundService
        {
            private readonly ILogger<PeriodicReadyCheckService> logger;

            public PeriodicReadyCheckService(ILogger<PeriodicReadyCheckService> logger)
            {
                this.logger = logger;
            }
            
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                logger.LogInformation(
                    "PeriodicReadyCheckService running.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await GetReadyResponse(stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogError("GetReadyResponse threw an exception. Sleep 10. {Exception}", e.ToString());
                        await Task.Delay(10 * 1000, stoppingToken);
                    }
                }
            }

            private async Task GetReadyResponse(CancellationToken stoppingToken)
            {
                var readyUrl = "http://localhost:5002/healthcheck/ready";
                var startupDelaySeconds = 15;
                var unhealthyDelaySeconds = 5;
                var healthyDelaySeconds = 30;
                
                HttpClient client = new HttpClient();
                var currentDelay = startupDelaySeconds;
                var unhealthyCount = 0;
                while (true)
                {
                    logger.LogDebug($"Delay {currentDelay}");
                    await Task.Delay(currentDelay * 1000, stoppingToken);
                    if (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    var response = await client.GetAsync(readyUrl, stoppingToken);
                    if (response != null)
                    {
                        logger.LogDebug($"{readyUrl} response {response.StatusCode}");
                        if (response.IsSuccessStatusCode)
                        {
                            readyResponse = await response.Content.ReadAsStringAsync();
                            currentDelay = healthyDelaySeconds;
                            unhealthyCount = 0;
                        }
                        else
                        {
                            readyResponse = null;
                            unhealthyCount++;
                        }
                    }
                    else
                    {
                        logger.LogError($"Could not connect to {readyUrl}");
                        readyResponse = null;
                        unhealthyCount++;
                    }

                    if (unhealthyCount > 0)
                    {
                        // back off
                        currentDelay = unhealthyDelaySeconds * unhealthyCount;
                    }
                }
            }
            
            public override async Task StopAsync(CancellationToken stoppingToken)
            {
                logger.LogInformation(
                    "PeriodicReadyCheckService is stopping.");

                await Task.CompletedTask;
            }
        }
    }
}
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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Prometheus;

public class RequestMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
    private readonly string counter_metric_name = "http_server_requests";
    private readonly string histo_metric_name = "http_server_requests_duration";

    public RequestMiddleware(
        RequestDelegate next
        , ILoggerFactory loggerFactory
    )
    {
        _next = next;
        _logger = loggerFactory.CreateLogger<RequestMiddleware>();
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;
        var method = httpContext.Request.Method;
        var labelNames = new[] {"path", "method", "status"};

        var counter = Metrics.CreateCounter(counter_metric_name, "HTTP Requests Total",
            new CounterConfiguration
            {
                LabelNames = labelNames
            });
        var histo = Metrics.CreateHistogram(histo_metric_name, "HTTP Request Duration",
            labelNames);

        var statusCode = 200;
        var stopWatch = Stopwatch.StartNew();
        try
        {
            await _next.Invoke(httpContext);
        }
        catch (Exception)
        {
            statusCode = 500;
            counter.Labels(path, method, statusCode.ToString()).Inc();
            throw;
        }
        finally
        {
            stopWatch.Stop();
        }

        if (path != "/metrics" && path != "/alive" && path != "/ready")
        {
            statusCode = httpContext.Response.StatusCode;
            counter.Labels(path, method, statusCode.ToString()).Inc();
            histo.Labels(path, method, statusCode.ToString())
                .Observe(stopWatch.Elapsed.TotalSeconds);
        }
    }
}

public static class RequestMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestMiddleware>();
    }
}
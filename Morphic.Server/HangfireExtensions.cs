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
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using Prometheus;

namespace Morphic.Server
{
    public class HangfireJobMetrics : JobFilterAttribute, IApplyStateFilter
    {
        private const string HangfireJobMetricHistogramName = "hangfire_job_duration";

        private static readonly Histogram HangfireJobHistogram = Metrics.CreateHistogram(
            HangfireJobMetricHistogramName,
            "Time of job execution",
            new HistogramConfiguration()
            {
                Buckets = Histogram.ExponentialBuckets(start: 0.5, factor: 2, count: 10),
                LabelNames = new[] {"jobName", "status"}
            });
        
        private static readonly string HangfireJobMetricRetryCounter = "hangfire_job_retry_count";

        private static readonly Counter HangfireJobRetryCounter = Metrics.CreateCounter(HangfireJobMetricRetryCounter,
            "Number of retries",
            new CounterConfiguration
            {
                LabelNames = new[] {"jobName"}
            });

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var contextType = context.NewState.GetType();
            var jobName = context.BackgroundJob.Job.Method.DeclaringType?.FullName!;
            if (contextType == typeof(FailedState) || contextType == typeof(SucceededState))
            {
                var elapsed = DateTime.UtcNow - context.BackgroundJob.CreatedAt;
                HangfireJobHistogram.Labels(jobName, context.NewState.Name)
                    .Observe(elapsed.TotalSeconds);
            }
            else if (contextType == typeof(ScheduledState) && context.OldStateName == "Processing")
            {
                // we've moved back to scheduled. We're retrying
                HangfireJobRetryCounter.Labels(jobName).Inc();
            }
        }
    
        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            Logger.Debug($"Changing State from {context.OldStateName} to {context.NewState}. CreatedAt {context.BackgroundJob.CreatedAt}");
        }
    }

}
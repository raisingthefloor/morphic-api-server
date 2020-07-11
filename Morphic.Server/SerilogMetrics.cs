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

using Prometheus;
using Serilog.Core;
using Serilog.Events;

namespace Morphic.Server
{
    class SerilogMetrics : ILogEventEnricher
    {
        private static readonly string counter_metric_name = "logs_log_level_count";
        private static readonly Counter SerilogLevelCounter = Metrics.CreateCounter(counter_metric_name,
            "Count of logging levels",
            new CounterConfiguration
            {
                LabelNames = new[] {"level"}
            });


        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            SerilogLevelCounter.Labels(logEvent.Level.ToString()).Inc();
        }
    }
}
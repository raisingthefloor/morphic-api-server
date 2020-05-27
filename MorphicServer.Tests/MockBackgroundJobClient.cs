using System;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace MorphicServer.Tests
{
    public class MockBackgroundJobClient : IBackgroundJobClient
    {
        public Job Job { get; set; }

        public string Create(Job job, IState state)
        {
            Job = job;
            return job.Method.Name;
        }

        public bool ChangeState(string jobId, IState state, string expectedState)
        {
            throw new NotImplementedException();
        }
    }
}
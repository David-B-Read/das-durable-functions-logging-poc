using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SFA.DAS.DurableFunction.Logging.POC;

namespace SFA.DAS.DurableFunction.POC
{
    public static class LoggingFunctionOrchestrator
    {

        [FunctionName("LoggingFunction")]
        public static async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var logger = TracingLoggerFactory.GetTracingLogger(log);

            logger.LogInformation($"Started orchestrator with ID {context.InstanceId}");
            var retryPolicy = new RetryOptions(new TimeSpan(0,0,0,1), 10);
            retryPolicy.BackoffCoefficient = 2;

            var learners = await context.CallActivityAsync<List<Learner>>("GetLearners", null);
            if (learners.Count > 0)
            {
                logger.LogInformation($"{learners.Count} learners to be processed");
                
                return;
            }
        }

        [FunctionName("GetLearners")]
        public static async Task<List<Learner>> GetLearners([ActivityTrigger] string name, ILogger log)
        {
            var logger = TracingLoggerFactory.GetTracingLogger(log);

            var fixture = new Fixture();
            var learners = fixture.CreateMany<Learner>(3).ToList();
            foreach (var learner in learners)
            {
                logger.LogInformation("Created learner", learner.Uln.ToString(), learner.LegalEntityId.ToString());
            }

            return await Task.FromResult(learners);
        }
               

        [FunctionName("PaymentOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "orchestrators/{functionName}/{instanceId}")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            string functionName,
            string instanceId,
            ILogger log)
        {
            var logger = TracingLoggerFactory.GetTracingLogger(log);

            var existingInstance = await starter.GetStatusAsync(instanceId);
            if (existingInstance == null)
            {
                return await Start(starter, logger, req, instanceId, functionName);
            }
            else if (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                existingInstance.RuntimeStatus = OrchestrationRuntimeStatus.Pending;
                return await Start(starter, logger, req, instanceId, functionName);
            }

            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent($"An instance with ID '{instanceId}' already exists."),
            };
        }

        private async static Task<HttpResponseMessage> Start(IDurableOrchestrationClient starter, ITracingLogger logger, HttpRequestMessage req, string instanceId, string functionName)
        {
            await starter.StartNewAsync(functionName, instanceId);

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return await Task.FromResult(starter.CreateCheckStatusResponse(req, instanceId));
        }

        public class Learner
        {
            public long Uln { get; set; }
            public long LegalEntityId { get; set; }
        }

    }
}
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
using SFA.DAS.DurableFunction.Logging.POC;

namespace SFA.DAS.DurableFunction.POC
{
    public class LoggingFunctionOrchestrator
    {
        private readonly ITracingLogger _logger;

        public LoggingFunctionOrchestrator(ITracingLogger logger)
        {
            _logger = logger;
        }

        [FunctionName("LoggingFunction")]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            _logger.LogInformation($"Started orchestrator with ID {context.InstanceId}");
            var retryPolicy = new RetryOptions(new TimeSpan(0,0,0,1), 10);
            retryPolicy.BackoffCoefficient = 2;

            var learners = await context.CallActivityAsync<List<Learner>>("GetLearners", null);
            if (learners.Count > 0)
            {
                _logger.LogInformation($"{learners.Count} learners to be processed");
                
                return;
            }
        }

        [FunctionName("GetLearners")]
        public async Task<List<Learner>> GetLearners([ActivityTrigger] string name)
        {
            var fixture = new Fixture();
            var learners = fixture.CreateMany<Learner>(3).ToList();
            foreach (var learner in learners)
            {
                _logger.LogInformation("Created learner", learner.Uln.ToString(), learner.LegalEntityId.ToString());
            }

            return await Task.FromResult(learners);
        }

        [FunctionName("ErrorFunction")]
        public async Task RunErrorOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            _logger.LogInformation($"Started orchestrator with ID {context.InstanceId}");
            var retryPolicy = new RetryOptions(new TimeSpan(0, 0, 0, 3), 10)
            {
                BackoffCoefficient = 2
            };

            try
            {
                var learners = await context.CallActivityWithRetryAsync<List<Learner>>("GetLearnersError", retryPolicy, null);
            }
            catch (FunctionFailedException functionFailedException)
            {
                //
                // Would expect function failed exception to bubble up to orchestrator,
                // but this doesn't happen when using retry async
                //
                var canProceed = retryPolicy.Handle(functionFailedException);
                if (canProceed)                
                { 
                    _logger.LogInformation("Unable to retrieve learners");
                   // context.
                }
                else
                {
                    _logger.LogError("Final attempt to retrieve learners failed", functionFailedException);
                }
            }
            catch (Exception unexpectedException)
            {
                _logger.LogError("Unable to retrieve learners", unexpectedException);
                throw;
            }
        }

        [FunctionName("GetLearnersError")]
        public async Task<List<Learner>> GetLearnersError([ActivityTrigger] string name)
        {
            throw new Exception($"Error generated {Guid.NewGuid()}");           
        }


        [FunctionName("PaymentOrchestrator_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "orchestrators/{functionName}/{instanceId}")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            string functionName,
            string instanceId)
        {            
            var existingInstance = await starter.GetStatusAsync(instanceId);
            if (existingInstance == null)
            {
                return await Start(starter, req, instanceId, functionName);
            }
            else if (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                existingInstance.RuntimeStatus = OrchestrationRuntimeStatus.Pending;
                return await Start(starter, req, instanceId, functionName);
            }

            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent($"An instance with ID '{instanceId}' already exists."),
            };
        }

        private async Task<HttpResponseMessage> Start(IDurableOrchestrationClient starter, HttpRequestMessage req, string instanceId, string functionName)
        {
            await starter.StartNewAsync(functionName, instanceId);

            _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return await Task.FromResult(starter.CreateCheckStatusResponse(req, instanceId));
        }

        public class Learner
        {
            public long Uln { get; set; }
            public long LegalEntityId { get; set; }
        }

    }
}
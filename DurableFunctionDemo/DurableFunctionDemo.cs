using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionDemo
{
    public static class DurableFunctionDemo
    {
        [FunctionName("OrchestrationFxn")]
        public static async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var name = context.GetInput<Data>()?.Name;
            await context.CallActivityAsync("ActivityFxn", name);
            // Not necessarily what needs to be returned here
            return "Completed";
        }

        [FunctionName("ActivityFxn")]
        public static string DoWork([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            var data = context.GetInput<string>();
            for (var i = 0; i < 4; i++)
            {
                log.LogInformation($"Activity started for Item:{data} and iteration : {i}");
            }
            // Not necessarily what needs to be returned here
            return $"Activity Processed for {data}!";
        }

        [FunctionName("TriggerFxn_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = null;

            var content = await req.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<string>>(content);
            foreach (var item in data)
            {
                // Function input comes from the request content.
                instanceId = await starter.StartNewAsync("OrchestrationFxn", new Data { Name = item });
                log.LogInformation($"Started orchestration for item:{item} with ID = '{instanceId}'.");

            }

            // Not necessarily what needs to be returned here
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("TriggerFxn_TimerTriggerStart")]
        public static async Task<HttpResponseMessage> TimerStart([TimerTrigger("0 0 22 28 - 31 * *")] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = null;

            var content = await req.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<string>>(content);
            foreach (var item in data)
            {
                // Function input comes from the request content.
                instanceId = await starter.StartNewAsync("OrchestrationFxn", new Data
                {
                    Name = item
                });
                log.LogInformation($"Started orchestration for item:{item} with ID = '{instanceId}'.");

            }

            // Not necessarily what needs to be returned here
            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        public class Data
        {
            public string Name { get; set; }
        }

    }
}
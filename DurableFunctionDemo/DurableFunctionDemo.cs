using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionDemo
{
    public class DurableFunctionDemo
    {
        [FunctionName("OrchestrationFxn")]
        public async Task<string> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var requestData = context.GetInput<Data>();
            var response = await context.CallActivityAsync<bool>("ActivityFxn", requestData);

            // While the activity returns a true value, re-trigger the activity function
            while(response)
            {
                response = await context.CallActivityAsync<bool>("ActivityFxn", requestData);
            }
           
            // Not necessarily what needs to be returned here
            return "Completed";
        }

        [FunctionName("ActivityFxn")]
        public bool DoWork([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            var name = context.GetInput<Data>()?.Name;
            var startDate = context.GetInput<Data>().startDate.ToString();
            var endDate = context.GetInput<Data>().endDate.ToString();

            for (var i = 0; i < 4; i++)
            {
                log.LogInformation($"Activity started for Item:{name} : Start Date : {startDate} : End Date : {endDate} and iteration : {i}");
            }

            // Create random boolean return value
            Random rnd = new Random();
            var rtn = rnd.NextDouble() > 0.5;

           return rtn;
        }

        [FunctionName("TriggerFxn_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
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
        public static async Task TimerStart([TimerTrigger("* * * * *")] TimerInfo timer, [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            var requestData = new Data { Name = "Test Data Name" };

            await starter.StartNewAsync("OrchestrationFxn", requestData);

        }

        public class Data
        {
            public string Name { get; set; }
            public DateTime startDate
            {
                // Calculate the first day of the previous month
                get { return new DateTime(DateTime.Now.Year, DateTime.Now.AddMonths(-1).Month, 1).Date; }
            }
            public DateTime endDate
            {
                // Cal
                get { return new DateTime(DateTime.Now.Year, DateTime.Now.AddMonths(-1).Month, new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1).Day).Date; }
            }

        }

    }
}
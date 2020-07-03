using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public interface ICounter
    {
        void Increment();
        void Reset();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Counter : ICounter
    {
        [JsonProperty("value")]
        public int CurrentValue { get; set; }
        public void Increment() => this.CurrentValue += 1;
        public void Reset() => this.CurrentValue = 0;

        [FunctionName(nameof(Counter))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<Counter>();
    }

    public static class DurableEntityCSharp
    {

        [FunctionName("DurableEntityCSharp_HttpClient")]
        public static async Task<IActionResult> HttpClient(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route="counters/{id}")]
                HttpRequest req,
            [DurableClient] IDurableEntityClient client,
            string id,
            ILogger log)
        {
            var operation = req.Query["operation"];
            var entityId = new EntityId("Counter", id);

            if (string.IsNullOrEmpty(operation))
            {
                // retrieve current value
                var response = await client.ReadEntityStateAsync<Counter>(entityId);
                if (response.EntityExists)
                {
                    return new OkObjectResult(response.EntityState);
                }
                else
                {
                    return new NotFoundResult();
                }
            }
            else if (operation == "increment")
            {
                // increment value
                await client.SignalEntityAsync<ICounter>(entityId, proxy => proxy.Increment());
                return new OkResult();
            }
            else if (operation == "reset")
            {
                // reset value
                await client.SignalEntityAsync<ICounter>(entityId, proxy => proxy.Reset());
                return new OkResult();
            }

            return new BadRequestResult();
        }
    }
}
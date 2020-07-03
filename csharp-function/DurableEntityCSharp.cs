using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Company.Function
{

    public static class DurableEntityCSharp
    {
        [FunctionName("Counter")]
        public static void Counter([EntityTrigger] IDurableEntityContext ctx)
        {
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "add":
                    ctx.SetState(ctx.GetState<int>() + ctx.GetInput<int>());
                    break;
                case "reset":
                    ctx.SetState(0);
                    break;
            }
        }

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
                var response = await client.ReadEntityStateAsync<int?>(entityId);
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
                await client.SignalEntityAsync(entityId, "add", 1);
                return new OkObjectResult("OK");
            }
            else if (operation == "reset")
            {
                // reset value
                await client.SignalEntityAsync(entityId, "reset");
                return new OkObjectResult("OK");
            }

            return new BadRequestResult();
        }
    }
}
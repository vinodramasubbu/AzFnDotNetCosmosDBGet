using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

namespace Claims.Function
{
    public class Claims1 { 
            public string id { get; set; }
            public string claimId { get; set; }
            public string dateOfClaim { get; set; }
            public string insuredName { get; set; }
            public string claimType { get; set; }
            public string claimStatus { get; set; }
        }
    public static class Claims
    {
        [FunctionName("Claims")]
        public static async Task<IActionResult> Run(
        //public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string claimId = req.Query["claimId"];
            log.LogInformation("C# HTTP trigger function processed a request.");
            //Cosmos DB Endpoint and Key
            var cosmosDBEndpoint = "https://xxxxxxxxxxxxx.documents.azure.com:443";
            var cosmosDBKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            // Database Details
            var databaseId = "claimsdb";
            var containerId = "claimcontainer";
            var partitionKeyPath = "/id";
            CosmosClient client = new CosmosClient(
                accountEndpoint: cosmosDBEndpoint,
                authKeyOrResourceToken: cosmosDBKey
                );
            Database database = client.GetDatabase(id: databaseId);
            Console.WriteLine($"New database:\t{database.Id}");
            Container container = await database.CreateContainerIfNotExistsAsync(
                id: containerId,
                partitionKeyPath: "/claimId"
            );
            Console.WriteLine($"New container:\t{container.Id}");

            // Create query using a SQL string and parameters
            var query = new QueryDefinition(
                query: "SELECT * FROM c WHERE c.claimId = @claimsId"
            )
                .WithParameter("@claimsId", claimId);

            using FeedIterator<Claims1> feed = container.GetItemQueryIterator<Claims1>(
                queryDefinition: query
            );

            List<Claims1> results = new();
            while (feed.HasMoreResults)
            {
                FeedResponse<Claims1> response = await feed.ReadNextAsync();
                foreach (Claims1 item in response)
                {
                    Console.WriteLine($"Found item:\t{item.claimId}\t{item.insuredName}\t{item.claimType}\t{item.claimStatus}");
                    results.Add(item);
                }
            }
            //return new OkResult();
            return (ActionResult)new OkObjectResult(results);
        }
    }
}

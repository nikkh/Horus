using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace Horus.Inspector
{

    public static class HttpInspector
    {
        
        [FunctionName("HttpInspector")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var responseMessage = $"Inspector Function triggered by HttpRequest at: {DateTime.Now}";
            log.LogInformation(responseMessage);

            var inspector = new Inspector(log);
            var results = await inspector.Inspect();

            return new OkObjectResult($"Inspector: You have a score of {results.Sum(s => s.Score)}");
        }
    }
}

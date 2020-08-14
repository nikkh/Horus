using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Horus.Inspector
{
    public static class TimedInspector
    {
        
        [FunctionName("TimedInspector")]
        public static async Task  Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {

            var responseMessage = $"Inspector Function triggered by Timer at: {DateTime.Now}";
            log.LogInformation(responseMessage);

            var inspector = new Inspector(log);
            _ = await inspector.Inspect();

        }
    }
}

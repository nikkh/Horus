using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Horus.Inspector
{
    public static class TimedInspector
    {
        
        [FunctionName("TimedInspector")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            

            
            log.LogInformation($"Inspector Function triggered by Timer at: {DateTime.Now}");

        }
    }
}

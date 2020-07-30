using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Functions.Engines
{
    public class HorusIntegrationEngine : IntegrationEngine
    {
        public async override Task<DocumentProcessingJob> Integrate(DocumentProcessingJob job, ILogger log, string snip)
        {
            job.Notes.Add("Note added by Horus Integration Engine");
            log.LogDebug($"{snip} HorusIntegrationEngine doesn't do anything, but you can develop your own and plug it in (e.g. send a service bus message, call a logic app)?");
            return job;
        }
    }
}

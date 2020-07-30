using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Functions.Engines
{
    public abstract class IntegrationEngine : IIntegrationEngine
    {   
        public abstract Task<DocumentProcessingJob> Integrate(DocumentProcessingJob job, ILogger log, string snip);
    }
}

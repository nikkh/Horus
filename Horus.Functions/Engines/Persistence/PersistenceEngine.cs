using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Functions.Engines
{
    public abstract class PersistenceEngine : IPersistenceEngine
    {
        
        
      
        public abstract Task<DocumentProcessingJob> Save(DocumentProcessingJob job, ILogger log, string snip);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public abstract class BaseJob
    {
        public BaseJob()
        {
            Notes = new List<string>();
        }
        public string OrchestrationId { get; set; }
        public string JobBlobName { get; set; }
        public Exception Exception { get; set; }
        public long Ticks { get; set; }
        public string OrchestrationContainerName { get; set; }
        public List<string> Notes { get; set; }
        public string RecognizerStatusUrl { get; set; }
        public string LatestRecognizerStatus { get; set; }
        public string RecognizerResponse { get; set; }
        public string DocumentFormat { get; set; }

    }
}

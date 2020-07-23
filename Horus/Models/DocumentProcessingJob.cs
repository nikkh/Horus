using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Horus.Functions.Models
{
    public class DocumentProcessingJob
    {
        public string StagingBlobUrl { get; set; }
        public string OrchestrationBlobUrl { get; set; }
        public string OrchestrationId { get; set; }
        public string OrchestrationContainerName { get; set; }
        public string OrchestrationBlobName { get;  set; }
        public string DocumentFormat { get; set; }
        public string ContentType { get;  set; }
        public string Thumbprint { get;  set; }
        public string RecognizerStatusUrl { get; set; }
        public ModelTraining Model { get; set; }
        public string RecognizerResponse { get; set; }
        public string RecognizedBlobName { get;  set; }
        public string LatestRecognizerStatus { get;  set; }
        public string JobBlobName { get;  set; }
        public string DocumentName { get;  set; }
        public string DocumentBlobUrl { get;  set; }
       
    }
}

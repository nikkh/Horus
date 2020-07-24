using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Horus.Functions.Models
{
    public class ModelTrainingJob
    {
        public string OrchestrationId { get; set; }
        public string BlobSasUrl { get; set; }
        public string BlobFolderName { get;  set; }
        public string IncludeSubFolders { get;  set; }
        public string UseLabelFile { get;  set; }
        public string RecognizerStatusUrl { get;  set; }
        public string LatestRecognizerStatus { get;  set; }
        public string RecognizerResponse { get;  set; }
        public string ModelId { get; set; }
        public string ModelVersion { get; set; }
        public DateTime CreatedDateTime { get;  set; }
        public DateTime UpdatedDateTime { get;  set; }
        public decimal AverageModelAccuracy { get;  set; }
        public string TrainingDocumentResults { get;  set; }
        public string DocumentFormat { get;  set; }
       
        public string JobBlobName { get;  set; }
        public Exception Exception { get; set; }
        public long Ticks { get; set; }
        public string OrchestrationContainerName { get; set; }
    }
}

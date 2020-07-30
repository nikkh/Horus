using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Horus.Functions.Models
{
    public class DocumentProcessingJob : BaseJob
    {
        public DocumentProcessingJob() : base()
        {
            
        }
        public string StagingBlobUrl { get; set; }
        public string OrchestrationBlobUrl { get; set; }
       
        public string OrchestrationBlobName { get;  set; }

        public string ContentType { get;  set; }
        public string Thumbprint { get;  set; }
        
        public ModelTrainingRecord Model { get; set; }
        
        public string RecognizedBlobName { get;  set; }
        
        public string DocumentName { get;  set; }
        public string DocumentBlobUrl { get;  set; }
        


    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Horus.Functions.Models
{
    public class ModelTrainingJob : BaseJob
    {
        public ModelTrainingJob() : base() { }
 
        public string BlobSasUrl { get; set; }
        public string BlobFolderName { get;  set; }
        public string IncludeSubFolders { get;  set; }
        public string UseLabelFile { get;  set; }

        public string ModelId { get; set; }
        public string ModelVersion { get; set; }
        public DateTime CreatedDateTime { get;  set; }
        public DateTime UpdatedDateTime { get;  set; }
        public decimal AverageModelAccuracy { get;  set; }
        public string TrainingDocumentResults { get;  set; }

       
     
    }
}

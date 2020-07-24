using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Trainer
{
    public class TrainingRequest
    {
        public List<TrainingRequestItem> Items { get; set; }
        public string ServiceBusConnectionString { get; set; }
        public string TrainingQueueName { get; set; }
        public string SasUrl { get; set; }
    }

    public class TrainingRequestItem
    {
        public string DocumentFormat { get; set; }
        public string BlobFolder { get; set; }
    }
}

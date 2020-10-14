using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class TrainingResponseMessage
    {
        public List<TrainingRequestMessage> TrainingRequestMessages { get; set; }
        public string ResponseMessage { get; set; }
    }
    // deployme
}

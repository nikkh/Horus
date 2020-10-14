using Microsoft.AspNetCore.Mvc.Formatters.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class TrainingResponseMessage
    {

        public TrainingResponseMessage() 
        {
            TrainingRequestMessages = new List<TrainingRequestMessage>();
        }
        public List<TrainingRequestMessage> TrainingRequestMessages { get; set; }
        public string ResponseMessage { get; set; }
    }
}

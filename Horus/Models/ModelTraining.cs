using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class ModelTraining
    {
        public string DocumentFormat { get; set; }
        public string ModelId { get; set; }
        public int ModelVersion { get; set; }
        public DateTime UpdatedDateTime { get; set; }
        public decimal AverageModelAccuracy { get; set; }
    }
}

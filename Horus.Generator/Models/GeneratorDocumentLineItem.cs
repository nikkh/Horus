using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.Models
{
    public class GeneratorDocumentLineItem
    {
        
            public string ItemNumber { get; set; }
            public string Title { get; set; }
        public string Author { get; set; }

        public string Isbn { get; set; }
        public double Quantity { get; set; }
        public double Discount { get; set; }
        public double Price { get; set; }
        public string PercentageTax { get; set; }
        public string Taxable { get; set; }
        public string ExtendedPrice { get; set; }
        public string ExtendedPricePlusTax { get; set; }
       
    }
}

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
        
        public bool Taxable { get; set; }
        
        public double GoodsValue
        {
            get
            {
                return Quantity * Price;
            }
        }
        public double DiscountValue {
            get
            {
                return GoodsValue * (Discount / 100);
            }
        }
        public double DiscountedGoodsValue {
            get
            {
                return GoodsValue - DiscountValue;
            }
        }
        public double TaxableValue {
            get
            {
                if (Taxable)
                {
                    return DiscountedGoodsValue;
                }
                return 0;
            }
        }

        

       
    }
}

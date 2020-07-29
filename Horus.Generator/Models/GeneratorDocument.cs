using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.Models
{
    public class GeneratorDocument
    {
        public List<GeneratorDocumentLineItem> Lines { get; set; }
        public string Account { get; set; }
        public string SingleName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Notes { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentDate { get; set; }

        public double PreTaxTotalValue
        {
            get
            {
                return Lines.Sum(l => l.DiscountedGoodsValue);
            }
        }
        public double TaxTotalValue
        {
            get
            {
                return Lines.Where(l => l.Taxable).Sum(l => l.DiscountedGoodsValue) * .19;
            }
        }
        public double ShippingTotalValue
        {
            get
            {
                return PreTaxTotalValue * .15;
            }
        }
        public double GrandTotalValue 
        {
            get 
            {
                return PreTaxTotalValue + TaxTotalValue + ShippingTotalValue;
            } 
        }
    }
}

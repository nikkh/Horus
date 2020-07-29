using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.ReferenceData
{
    public class Suppliers
    {
        private Dictionary<int, Supplier> suppliers = new Dictionary<int, Supplier>();
        public Suppliers()
        {
            suppliers.Add(1, new Supplier
            {
                LogoFile = "../../images/oscorp.jpg",
                SupplierName = "Oscorp Chemicals | 14 Darlington St | Wolverhampton | WV1 2DC",
                SupplierFullName = "Oscorp Chemicals | 14 Darlington St | Wolverhampton | WV1 2DC | 01902 887887",
                SupplierKey = "oscorp",
                BuilderAssembly = "Horus.Generator",
                BuilderType = "Builders.OscorpDocumentBuilder",
                MaxLines = 6
            });
 
            suppliers.Add(2, new Supplier
            {
                LogoFile = "../../images/OIP.jpg",
                SupplierName = "ABC Generics | 42 Reform Street, Rushall | Walsall WS8 4BX",
                SupplierFullName = "ABC Generics | 42 Reform Street, Rushall | Walsall WS8 4BX, United Kingdom | 01922 219912",
                SupplierKey = "abc",
                BuilderAssembly = "Horus.Generator",
                BuilderType = "Builders.ABCDocumentBuilder",
                MaxLines = 20
            });
            suppliers.Add(3, new Supplier
            {
                LogoFile = "../../images/Nouryon.png",
                SupplierName = "Nouryon Inc � Sample Street 42 � 56789 Cologne",
                SupplierFullName = "Nouryon Inc � Sample Street 42 � 56789 Cologne � Germany",
                SupplierKey = "Nouryon",
                BuilderAssembly = "Horus.Generator",
                BuilderType = "Builders.NouryonDocumentBuilder",
                MaxLines = 15
            });
        }
        public Supplier GetRandomSupplier()
        {
            Random r = new Random();
            int i = r.Next(1, suppliers.Count);
                return suppliers[i];
        }

        public List<Supplier> GetSuppliers() 
        {
            var retval = new List<Supplier>();
            foreach (var item in suppliers)
            {
                retval.Add(item.Value);
            }
            return retval;
            
        }
    }

    public class Supplier
    {
        public string SupplierKey { get; set; }
        public string SupplierName { get; set; }
        public string SupplierFullName { get; set; }
        public string LogoFile { get; set; }

        public string BuilderAssembly { get; set; }

        public string BuilderType { get; set; }
        public int MaxLines { get;  set; }
    }
}

using System.Collections.Generic;

namespace Horus.Generator.Models
{
    public class GeneratorHeader
    {
        public string SupplierKey { get; set; }
        public Dictionary<string, string> SupplierNameProperties { get; set; }

        public string SupplierName { get; set; }
        public string SupplierFullName { get; set; }
        public string LogoFile { get; set; }
        public string DocumentType { get; set; }

        public string BuilderAssembly { get; set; }

        public string BuilderType { get; set; }
    }
}
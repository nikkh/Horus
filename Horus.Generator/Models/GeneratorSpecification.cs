using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Horus.Generator.Models
{
    public class GeneratorSpecification
    {
        public GeneratorHeader Header { get; set; }

        public List<GeneratorDocument> Documents {get; set; }
        

    }
}

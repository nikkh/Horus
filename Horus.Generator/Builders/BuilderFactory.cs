using Horus.Generator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Horus.Generator.Builders
{
    public static class BuilderFactory
    {
        public static IDocumentBuilder GetBuilder(string assembly, string type, GeneratorHeader header, GeneratorDocument generatorDocument) 
        {

            string typeToLoad = String.Format(@"{0}.{1}, {0}", assembly, type);
            Type builderType = Type.GetType(typeToLoad);

            if ((assembly != null) && (type != null))
            {
                object o = Activator.CreateInstance(builderType, new Object[] { header, generatorDocument });
                if (o is IDocumentBuilder)
                {
                    return (IDocumentBuilder)o;
                }
                throw new Exception("Specified Builder does not exist");
            }
            return null;

        }
    }
}

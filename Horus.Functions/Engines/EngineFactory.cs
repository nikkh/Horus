using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Engines
{
    public static class EngineFactory
    {
        public static IEngine GetEngine(string assembly, string type)
        {

            string typeToLoad = String.Format(@"{0}.{1}, {0}", assembly, type);
            Type builderType = Type.GetType(typeToLoad);

            if ((assembly != null) && (type != null))
            {
                object o = Activator.CreateInstance(builderType);
                if (o is IEngine)
                {
                    return (IEngine)o;
                }
                throw new Exception("Specified Engine does not exist");
            }
            return null;

        }
    }
}

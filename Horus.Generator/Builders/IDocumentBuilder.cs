using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MigraDoc.DocumentObjectModel;

namespace Horus.Generator.Builders
{
    public interface IDocumentBuilder
    {
        Document Build();
    }
}

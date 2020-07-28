#region MigraDoc - Creating Documents on the Fly
//
// Authors:
//   PDFsharp Team (mailto:PDFsharpSupport@pdfsharp.de)
//
// Copyright (c) 2001-2009 empira Software GmbH, Cologne (Germany)
//
// http://www.pdfsharp.com
// http://www.migradoc.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using Horus.Generator.Builders;
using Horus.Generator.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Newtonsoft.Json;

namespace Horus.Generator
{
    class Program
    {
        
        static void Main(string[] args)
        {

            var genSpec = Generator.Generate();

            //string spec = File.ReadAllText($"{Directory.GetCurrentDirectory()}\\Data\\{args[0]}");
            //GeneratorSpecification genSpec= JsonConvert.DeserializeObject<GeneratorSpecification>(spec);

            //genSpec.Header.SupplierNameProperties = new Dictionary<string, string>();
            //genSpec.Header.SupplierNameProperties.Add("paragraph.Format.Font.Size", "18");
            //genSpec.Header.SupplierNameProperties.Add("paragraph.Format.Font.Name", "Comic Sans MS");
            //genSpec.Header.SupplierNameProperties.Add("paragraph.Format.SpaceAfter", "3");

            var temp = JsonConvert.SerializeObject(genSpec);
           
            var outputDirName = $"{Directory.GetCurrentDirectory()}\\{genSpec.Header.SupplierKey}";
            if (!Directory.Exists(outputDirName))
            {
                Directory.CreateDirectory(outputDirName);
            }

            try
            {
                foreach (var item in genSpec.Documents)
                {
                    var documentBuilder = BuilderFactory.GetBuilder(genSpec.Header.BuilderAssembly, genSpec.Header.BuilderType, genSpec.Header, item);
                    var document = documentBuilder.Build();
                    document.UseCmykColor = true;
                    PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true)
                    {
                        Document = document
                    };
                    pdfRenderer.RenderDocument();

                    string filename = $"{outputDirName}\\{genSpec.Header.DocumentType}-{item.DocumentNumber}.pdf";
                    if (File.Exists(filename)) File.Delete(filename);
                    pdfRenderer.Save(filename);
#if DEBUG
                    Process.Start(filename);
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                throw ex;
            }
        }


    //
    }

    }

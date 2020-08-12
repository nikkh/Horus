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
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Horus.Generator.Builders;
using Horus.Generator.Models;
using Horus.Generator.ReferenceData;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Newtonsoft.Json;

namespace Horus.Generator
{
    class Program
    {
        const int DOCS_TO_GENERATE = 10;
        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            var docCount = 0;
            Console.WriteLine($"Welcome to Horus Document Generator!");
            Console.WriteLine($"today we will be generating {DOCS_TO_GENERATE} documents for each supplier");
                     
            foreach (var supplier in new Suppliers().GetSuppliers())
            {
                Console.WriteLine($"processing supplier {supplier.SupplierName.Split('|')[0]}");
                int nextDocNumber=15000;
                var outputDirName = $"{Directory.GetCurrentDirectory()}\\generated\\{supplier.SupplierKey.ToLower()}";
                if (!Directory.Exists(outputDirName))
                {
                    Directory.CreateDirectory(outputDirName);
                }
                else
                {
                    var pdfs = Directory.GetFiles(outputDirName);
                    nextDocNumber = Int32.Parse(pdfs.Last().Split('\\').Last().Split('.')[0].Split('-')[1]);
                }
                Console.WriteLine($"files will be saved to {outputDirName}");
                Console.WriteLine($"first document number for this run will be {nextDocNumber+1}");
                Console.WriteLine($"generating documents...");
                
                var genSpec = Generator.Generate(supplier, DOCS_TO_GENERATE, nextDocNumber);
                Console.WriteLine($"generating complete...");
                Console.WriteLine($"Building a document image for each of the generated documents");
                try
                {
                    int i = 0;
                    foreach (var item in genSpec.Documents)
                    {
                        i++;
                        docCount++;
                        var docStartTime = DateTime.Now;
                        Console.WriteLine($"Building image {i} of {genSpec.Documents.Count}");
                        var documentBuilder = BuilderFactory.GetBuilder(genSpec.Header.BuilderAssembly, genSpec.Header.BuilderType, genSpec.Header, item);
                        var document = documentBuilder.Build();
                        document.UseCmykColor = true;
                        Console.WriteLine($"Rendering image {i} to PDF");
                        PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true)
                        {
                            Document = document
                        };
                        pdfRenderer.RenderDocument();

                        string fileName = $"{genSpec.Header.DocumentType}-{item.DocumentNumber}.pdf";
                        string fileNameWithDirectory = $"{outputDirName}\\{fileName}";
                        Console.WriteLine($"Saving image {i} to {fileNameWithDirectory}");
                        if (File.Exists(fileNameWithDirectory)) File.Delete(fileNameWithDirectory);
                        pdfRenderer.Save(fileNameWithDirectory);
                        var docEndTime = DateTime.Now;
                        Console.WriteLine($"Generation of {fileName} took {(docEndTime - docStartTime).TotalMilliseconds} ms.");
#if DEBUG
                        // Process.Start(filename);
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
            var endTime = DateTime.Now;
            Console.WriteLine($"Generated {docCount} documents in {(endTime - startTime).TotalSeconds} seconds.");
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }

}

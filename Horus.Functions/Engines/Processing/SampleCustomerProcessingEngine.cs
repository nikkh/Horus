using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Horus.Functions.Engines
{
    public class SampleCustomerProcessingEngine : ProcessingEngine
    {
        public override Document Process(DocumentProcessingJob job, ILogger log, string snip)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Document document = new Document { FileName = job.JobBlobName };
            document.UniqueRunIdentifier = job.OrchestrationId;
            document.FileName = job.OrchestrationBlobName;
            JObject jsonContent = JObject.Parse(job.RecognizerResponse);
            if (jsonContent["status"] != null) document.RecognizerStatus = jsonContent["status"].ToString();
            if (jsonContent["errors"] != null) document.RecognizerErrors = jsonContent["errors"].ToString();

            // Fill out the document object
            var nittyGritty = (JObject)jsonContent["analyzeResult"]["documentResults"][0]["fields"];
            document.ShreddingUtcDateTime = DateTime.Now;
            document.OrderNumber = GetString(SampleCustomerConstants.OrderNumber, nittyGritty, document);
            document.OrderDate = GetDate(SampleCustomerConstants.OrderDate, nittyGritty, document);
            document.TaxDate = GetDate(SampleCustomerConstants.TaxDate, nittyGritty, document);
            document.DocumentNumber = GetString(SampleCustomerConstants.InvoiceNumber, nittyGritty, document);
            document.Account = GetString(SampleCustomerConstants.Account, nittyGritty, document);
            document.NetTotal = GetNumber(SampleCustomerConstants.NetTotal, nittyGritty, document) ?? 0;
            document.VatAmount = GetNumber(SampleCustomerConstants.VatAmount, nittyGritty, document) ?? 0;
            document.GrandTotal = GetNumber(SampleCustomerConstants.GrandTotal, nittyGritty, document) ?? 0;
            document.PostCode = GetString(SampleCustomerConstants.PostCode, nittyGritty, document);
            document.TimeToShred = 0; // Set after processing complete
            document.Thumbprint = job.Thumbprint;
            document.ModelId = job.Model.ModelId;
            document.ModelVersion = job.Model.ModelVersion.ToString(); ;
            if (document.TaxDate != null && document.TaxDate.HasValue)
            {
                document.TaxPeriod = document.TaxDate.Value.Year.ToString() + document.TaxDate.Value.Month.ToString();
            }

            // Lines

            for (int i = 1; i < SampleCustomerConstants.MAX_DOCUMENT_LINES; i++)
            {
                var lineNumber = i.ToString("D2");
                string lineItemId = $"{SampleCustomerConstants.LineItemPrefix}{lineNumber}";
                string unitPriceId = $"{SampleCustomerConstants.UnitPricePrefix}{lineNumber}";
                string quantityId = $"{SampleCustomerConstants.QuantityPrefix}{lineNumber}";
                string netPriceId = $"{SampleCustomerConstants.NetPricePrefix}{lineNumber}";
                string vatCodeId = $"{SampleCustomerConstants.VatCodePrefix}{lineNumber}";

                // presence of any one of the following items will mean the document line is considered to exist.
                string[] elements = { unitPriceId, netPriceId, lineItemId };

                if (AnyElementsPresentForThisLine(nittyGritty, lineNumber, elements))
                {
                    log.LogTrace($"{snip}{lineItemId}: {GetString(lineItemId, nittyGritty, document)}");
                    DocumentLineItem lineItem = new DocumentLineItem();

                    // aid debug
                    string test = nittyGritty.ToString();
                    //
                    lineItem.ItemDescription = GetString(lineItemId, nittyGritty, document, DocumentErrorSeverity.Terminal);
                    lineItem.DocumentLineNumber = lineNumber;
                    lineItem.LineQuantity = GetNumber(quantityId, nittyGritty, document).ToString();
                    lineItem.NetAmount = GetNumber(netPriceId, nittyGritty, document, DocumentErrorSeverity.Terminal) ?? 0;
                    lineItem.UnitPrice = GetNumber(unitPriceId, nittyGritty, document, DocumentErrorSeverity.Terminal) ?? 0;
                    lineItem.VATCode = GetString(vatCodeId, nittyGritty, document, DocumentErrorSeverity.Warning);
                    document.LineItems.Add(lineItem);
                }
                else
                {
                    break;
                }
            }

            timer.Stop();
            document.TimeToShred = timer.ElapsedMilliseconds;
            return document;
        }
    }
}

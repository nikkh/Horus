using Horus.Functions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Horus.Functions.Engines
{
    public class HorusProcessingEngine : ProcessingEngine
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
            var temp = nittyGritty.ToString();
            document.ShreddingUtcDateTime = DateTime.Now;
            document.OrderNumber = GetString(HorusConstants.OrderNumber, nittyGritty, document);
            document.OrderDate = GetDate(HorusConstants.OrderDate, nittyGritty, document);
            document.TaxDate = GetDate(HorusConstants.TaxDate, nittyGritty, document);
            document.DocumentNumber = GetString(HorusConstants.InvoiceNumber, nittyGritty, document);
            document.Account = GetString(HorusConstants.Account, nittyGritty, document);
            document.NetTotal = GetNumber(HorusConstants.NetTotal, nittyGritty, document) ?? 0;
            document.VatAmount = GetNumber(HorusConstants.VatAmount, nittyGritty, document) ?? 0;
            document.ShippingTotal = GetNumber(HorusConstants.ShippingTotal, nittyGritty, document) ?? 0;
            document.GrandTotal = GetNumber(HorusConstants.GrandTotal, nittyGritty, document) ?? 0;
            document.PostCode = GetString(HorusConstants.PostCode, nittyGritty, document);
            document.TimeToShred = 0; // Set after processing complete
            document.Thumbprint = job.Thumbprint;
            document.ModelId = job.Model.ModelId;
            document.ModelVersion = job.Model.ModelVersion.ToString(); 
            if (document.TaxDate != null && document.TaxDate.HasValue)
            {
                document.TaxPeriod = document.TaxDate.Value.Year.ToString() + document.TaxDate.Value.Month.ToString();
            }

            // Lines

            for (int i = 1; i < HorusConstants.MAX_DOCUMENT_LINES; i++)
            {
                var lineNumber = i.ToString("D2");
                string lineItemId = $"{HorusConstants.LineItemPrefix}{lineNumber}";
                string unitPriceId = $"{HorusConstants.UnitPricePrefix}{lineNumber}";
                string quantityId = $"{HorusConstants.QuantityPrefix}{lineNumber}";
                string netPriceId = $"{HorusConstants.NetPricePrefix}{lineNumber}";
                string vatCodeId = $"{HorusConstants.VatCodePrefix}{lineNumber}";
                string discountPercentId = $"{HorusConstants.DiscountPercentPrefix}{lineNumber}";
                string taxableId = $"{HorusConstants.TaxablePrefix}{lineNumber}";

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
                    lineItem.DiscountPercent = GetNumber(discountPercentId, nittyGritty, document, DocumentErrorSeverity.Warning) ?? 0;
                    lineItem.Taxableindicator = GetString(taxableId, nittyGritty, document, DocumentErrorSeverity.Warning);
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

using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class ParsingConstants
    {
        public const string TaxDate = "TaxDate";
        public const string OrderNumber = "OrderNO";
        public const string OrderDate = "OrderDate";
        public const string InvoiceNumber = "Inv";
        public const string Account = "AccountNo";
        public const string LineItemPrefix = "Drug";
        public const int MAX_DOCUMENT_LINES = 50;
        public const string QuantityPrefix = "Qty";
        public const string UnitPricePrefix = "Unit";
        public const string NetPricePrefix = "Net";
        public const string VatCodePrefix = "Vat";
        public const string VatAmount = "VAT";
        public const string NetTotal = "Total";
        public const string GrandTotal = "TotalIncVAT";
        public const string PostCode = "PostCode";
        public const string IllegalCharacterMarker = "@Illegal@";
        public const string UniqueProcessingIdKey = "UniqueProcessingId";
        public const string InvoiceFormatKey = "InvoiceFormat";
        public const string RecognizedExtension = "-recognized.json";
        public const string ExceptionExtension = "-exception.json";
        public const string InvoiceExtension = "-invoice.json";
        public const string DocumentExtension = "-document.json";
        public const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public const string FormRecognizerApiPath = "formrecognizer/v2.0-preview/custom/models";
        public const string FormRecognizerAnalyzeVerb = "analyze";
        public static readonly string UniqueRunIdentifierKey = "UniqueRunIdentifier";
        public static readonly string TelemetryOperationParentIdKey = "TelemetryParentId";
        public static readonly string TelemetryOperationIdKey = "TelemetryOperationId";
        public static readonly string ThumbprintKey = "Thumbprint";
        public static readonly string ModelIdKey = "ModelId";
        public static readonly string ModelVersionKey = "ModelVersion";
        public const int MaxRetriesForBlobLease = 15;

        public static readonly string ProcessingJobFileExtension = ".processing.job.json";
        public static readonly string TrainingJobFileExtension = ".training.job.json";

        public static List<string> AllowedContentTypes = new List<string> { "image/jpeg", "image/png", "image/tiff" };
    }

    public static class PreprocessorStatus
    {
        private const string pending = "Pending";
        private const string completed = "Completed";
        public static string Pending { get { return pending; } }
        public static string Completed { get { return completed; } }
    }

    public static class TrainingStatus
    {
        private const string ready = "ready";
        private const string invalid = "invalid";
        public static string Ready { get { return ready; } }
        public static string Invalid { get { return invalid; } }
    }

    public static class RecognizerStatus
    {
        private const string succeeded = "succeeded";
        private const string failed = "failed";
        private const string notStarted = "notStarted";
        private const string running = "running";
        public static string Succeeded { get { return succeeded; } }
        public static string Failed { get { return failed; } }
        public static string NotStarted { get { return notStarted; } }
        public static string Running { get { return running; } }


    }
}
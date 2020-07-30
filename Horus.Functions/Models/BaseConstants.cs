using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class BaseConstants
    {
        public const int MAX_DOCUMENT_LINES = 50;
        public static List<string> AllowedContentTypes = new List<string> { "image/jpeg", "image/png", "image/tiff", "application/pdf" };
        public const string FormRecognizerApiPath = "formrecognizer/v2.0-preview/custom/models";
        public const string FormRecognizerAnalyzeVerb = "analyze";
        public const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";
        public const int MaxRetriesForBlobLease = 15;
        public static readonly string ProcessingJobFileExtension = ".processing.job.json";
        public static readonly string TrainingJobFileExtension = ".training.job.json";
        public const string RecognizedExtension = "-recognized.json";
        public const string ExceptionExtension = "-exception.json";
        public const string InvoiceExtension = "-invoice.json";
        public const string DocumentExtension = "-document.json";
        public const string IllegalCharacterMarker = "@Illegal@";
    }
}

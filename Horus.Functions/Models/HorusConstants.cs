using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public class HorusConstants : BaseConstants
    {
        public const string TaxDate = "TaxDate";
        public const string OrderNumber = "OrderNO";
        public const string OrderDate = "OrderDate";
        public const string InvoiceNumber = "Inv";
        public const string Account = "AccountNo";
        public const string ShippingTotal = "Shipping";
        public const string LineItemPrefix = "Drug";
        public const string QuantityPrefix = "Qty";
        public const string UnitPricePrefix = "Unit";
        public const string NetPricePrefix = "Net";
        public const string VatCodePrefix = "Vat";
        public const string DiscountPercentPrefix="Disc";
        public const string TaxablePrefix = "Taxable";
        public const string VatAmount = "VAT";
        public const string NetTotal = "Total";
        public const string GrandTotal = "TotalIncVAT";
        public const string PostCode = "PostCode";
        public const string UniqueProcessingIdKey = "UniqueProcessingId";
        public const string InvoiceFormatKey = "InvoiceFormat";
        public static readonly string UniqueRunIdentifierKey = "UniqueRunIdentifier";
        public static readonly string TelemetryOperationParentIdKey = "TelemetryParentId";
        public static readonly string TelemetryOperationIdKey = "TelemetryOperationId";
        public static readonly string ThumbprintKey = "Thumbprint";
        public static readonly string ModelIdKey = "ModelId";
        public static readonly string ModelVersionKey = "ModelVersion";
        
    }
}
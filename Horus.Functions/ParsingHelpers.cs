using Horus.Functions.Models;
using Newtonsoft.Json.Linq;
using System;

namespace Horus.Functions
{
public static class ParsingHelpers
{
    #region Document Parsing
    public static bool AnyElementsPresentForThisLine(JObject nittyGritty, string lineNumber, string[] elements)
    {

        foreach (JProperty child in nittyGritty.Children<JProperty>())
        {
            foreach (var elementName in elements)
            {

                if (child.Name == elementName)
                {
                    if (!string.IsNullOrEmpty(child.Value.ToString()))
                    {
                        return true;
                    }

                }
            }
        }

        return false;
    }

    public static string SafeString(string input)
    {
        return input.Replace("'", ParsingConstants.IllegalCharacterMarker);
    }
    public static string GetString(string elementId, JObject nittyGritty, Document document, DocumentErrorSeverity severity = DocumentErrorSeverity.Warning)
    {
        string value;
        try
        {
            value = SafeString(nittyGritty[elementId]["text"].ToString());
        }
        catch (NullReferenceException)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0001", ErrorSeverity = severity, ErrorMessage = SafeString($"GetString() Specified Element {elementId} is null") });
            return null;
        }
        catch (Exception)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0001", ErrorSeverity = severity, ErrorMessage = SafeString($"GetString() Specified Element {elementId} does not exist in recognized output") });
            return null;
        }
        return value;
    }

    public static Decimal? GetNumber(string elementId, JObject nittyGritty, Document document, DocumentErrorSeverity severity = DocumentErrorSeverity.Warning)
    {

        string numberAsString;
        try
        {
            numberAsString = nittyGritty[elementId]["text"].ToString();
        }
        catch (NullReferenceException)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0002", ErrorSeverity = severity, ErrorMessage = SafeString($"GetNumber() Specified Element {elementId} is null") });
            return null;
        }
        catch (Exception)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0002", ErrorSeverity = severity, ErrorMessage = SafeString($"GetNumber() Specified Element {elementId} does not exist in recognized output") });
            return null;
        }

        if (numberAsString == null)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0003", ErrorSeverity = severity, ErrorMessage = SafeString($"GetNumber() {elementId} exists but its value is null") });
            return null;
        }

        if (Decimal.TryParse(numberAsString.Trim().Replace(" ", string.Empty), out decimal numberValue))
        {
            if (numberValue == 0)
            {
                document.Errors.Add(new DocumentError { ErrorCode = "PRE0004", ErrorSeverity = DocumentErrorSeverity.Warning, ErrorMessage = SafeString($"GetNumber() {elementId} exists but its value is zero") });
            }

            return numberValue;
        }
        else
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0005", ErrorSeverity = severity, ErrorMessage = SafeString($"GetNumber() {elementId} exists but cannot be parsed as a number={numberAsString}") });
            return null;
        }

    }

    public static DateTime? GetDate(string elementId, JObject nittyGritty, Document document, DocumentErrorSeverity severity = DocumentErrorSeverity.Warning)
    {
        string dateAsString;
        try
        {
            dateAsString = nittyGritty[elementId]["text"].ToString();
        }
        catch (NullReferenceException)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0006", ErrorSeverity = severity, ErrorMessage = SafeString($"GetDate() Specified Element {elementId} is null") });
            return null;
        }
        catch (Exception)
        {
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0006", ErrorSeverity = severity, ErrorMessage = SafeString($"GetDate() Specified Element {elementId} does not exist in recognized output") });
            return null;
        }

        DateTime dateValue;
        if (DateTime.TryParse(dateAsString, out dateValue))
            return dateValue;
        else
        {
            string safeDateAsString = dateAsString.Replace("'", ParsingConstants.IllegalCharacterMarker);
            document.Errors.Add(new DocumentError { ErrorCode = "PRE0007", ErrorSeverity = severity, ErrorMessage = SafeString($"GetDate() Specified Element {elementId} does not contain a valid date: TaxDate={dateAsString}") });
            return null;
        }
    }
    #endregion

}
}



using System.Globalization;

namespace InvEntry.Utils;

public enum LabelPrintStatus
{
    Failed,
    Submitted
}

public sealed record LabelPrintResult(
    LabelPrintStatus Status,
    string? ErrorMessage = null)
{
    // Submitted means Windows accepted the RAW ZPL data. It does not prove that
    // the label physically came out of the printer.
    public bool Success => Status == LabelPrintStatus.Submitted;
}

public static class BarCodePrint
{
    private const string PrinterName = "Bar Code Printer TT065-50";

    // RawPrinterHelper and the initialization flag are shared by all callers.
    // This lock prevents two labels from being submitted at the same time.
    private static readonly object PrintLock = new();
    private static bool initialized;

    public static LabelPrintResult ProcessBarCode(
        string productCode,
        string productName,
        decimal vaPercent,
        decimal productWeight,
        decimal productStoneWeight,
        string productPurity,
        string companyName = "MATHA")
    {
        var validationError = ValidateInput(
            productCode,
            productWeight,
            productStoneWeight,
            vaPercent);

        if (validationError is not null)
            return Failed(validationError);

        try
        {
            lock (PrintLock)
            {
                if (!EnsurePrinterInitialized(out var initializationError))
                    return Failed(initializationError ?? "Printer initialization failed.");

                var productWeightText = productWeight.ToString(
                    "F3",
                    CultureInfo.InvariantCulture);

                var stoneWeightText = productStoneWeight > 0m
                    ? productStoneWeight.ToString("F3", CultureInfo.InvariantCulture)
                    : string.Empty;

                var vaPercentText =
                    $"{vaPercent.ToString("0.##", CultureInfo.InvariantCulture)}%";

                var zplCommand = GenerateZpl(
                    EscapeZpl(productCode),
                    EscapeZpl(productName),
                    vaPercentText,
                    productWeightText,
                    stoneWeightText,
                    EscapeZpl(productPurity),
                    EscapeZpl(companyName));

                if (!RawPrinterHelper.SendZPLToPrinter(
                        PrinterName,
                        zplCommand,
                        out var printError))
                {
                    return Failed(printError ?? "Windows rejected the print data.");
                }

                // RawPrinterHelper confirms only that the ZPL was submitted.
                return new LabelPrintResult(LabelPrintStatus.Submitted);
            }
        }
        catch (Exception ex)
        {
            return Failed($"Unexpected printing error: {ex.Message}");
        }
    }

    public static LabelPrintResult ReinitializePrinter()
    {
        try
        {
            lock (PrintLock)
            {
                initialized = false;

                if (!EnsurePrinterInitialized(out var error))
                    return Failed(error ?? "Printer reinitialization failed.");

                return new LabelPrintResult(LabelPrintStatus.Submitted);
            }
        }
        catch (Exception ex)
        {
            initialized = false;
            return Failed($"Printer reinitialization failed: {ex.Message}");
        }
    }

    private static bool EnsurePrinterInitialized(out string? error)
    {
        if (initialized)
        {
            error = null;
            return true;
        }

        var success = RawPrinterHelper.SendZPLToPrinter(
            PrinterName,
            GenerateInitZpl(),
            out error);

        // Never mark initialization complete when submission failed.
        initialized = success;
        return success;
    }

    private static string? ValidateInput(
        string productCode,
        decimal productWeight,
        decimal productStoneWeight,
        decimal vaPercent)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            return "Product code is required.";

        if (productWeight <= 0m)
            return "Product weight must be greater than zero.";

        if (productStoneWeight < 0m)
            return "Stone weight cannot be negative.";

        if (productStoneWeight > productWeight)
            return "Stone weight cannot be greater than product weight.";

        if (vaPercent < 0m)
            return "VA percentage cannot be negative.";

        return null;
    }

    private static LabelPrintResult Failed(string message) =>
        new(LabelPrintStatus.Failed, message);

    private static string EscapeZpl(string? value)
    {
        // Prevent field values from terminating ^FD or injecting ZPL commands.
        return (value ?? string.Empty)
            .Replace('^', ' ')
            .Replace('~', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }

    private static string GenerateInitZpl()
    {
        return "^XA\n" +
               "^MNA\n" +
               "^MTT\n" +
               "^SLC0\n" +
               "^JUS\n" +
               "^PR6\n" +
               "^MD30\n" +
               "^LH0,0\n" +
               "^FWN\n" +
               "^XZ\n";
    }

    private static string GenerateZpl(
        string productCode,
        string productName,
        string vaPercent,
        string productWeight,
        string stoneWeight,
        string productPurity,
        string companyName)
    {
        companyName = LimitLength(companyName, 20);
        productName = LimitLength(productName, 28);
        productPurity = LimitLength(productPurity, 15);

        var stoneText = stoneWeight.Length > 0
            ? $"Stone: {stoneWeight}"
            : string.Empty;

        return
            "^XA\n" +
            "^PW700\n" +
            "^LL250\n" +
            "^MD10\n" +

            "^FO5,5\n" +
            "^BY1,2.0,40\n" +
            "^BCN,40,N,N,N\n" +
            $"^FD{productCode}^FS\n" +

            "^FO5,55\n" +
            "^A0N,20,20\n" +
            $"^FD{productCode}^FS\n" +

            "^FO140,55\n" +
            "^A0N,18,18\n" +
            "^FD ^FS\n" +

            "^FO10,85\n" +
            "^A0N,18,18\n" +
            $"^FD{companyName}^FS\n" +

            "^FO250,5\n" +
            "^A0N,22,22\n" +
            $"^FD{productName}^FS\n" +

            "^FO250,25\n" +
            "^A0N,20,20\n" +
            $"^FDGwt: {productWeight}^FS\n" +

            "^FO250,45\n" +
            "^A0N,20,20\n" +
            $"^FD{stoneText}^FS\n" +

            "^FO250,65\n" +
            "^A0N,20,20\n" +
            $"^FDMC: {vaPercent}^FS\n" +

            "^FO250,85\n" +
            "^A0N,20,20\n" +
            $"^FDPurity: {productPurity}^FS\n" +
            "^XZ\n";
    }

    private static string LimitLength(string value, int maximumLength)
    {
        if (value.Length <= maximumLength)
            return value;

        return value[..maximumLength];
    }
}

public class BarCodeProductRec
{
    public string ProductCode { get; set; } = string.Empty;
}

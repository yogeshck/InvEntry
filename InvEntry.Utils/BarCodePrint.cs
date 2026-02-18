namespace InvEntry.Utils;

public class BarCodePrint
{

    private static string PrinterName = "Bar Code Printer TT065-50"; // Set your printer name
    private bool firstRec = true;

    public static bool ProcessBarCode(string productCode, string productName, decimal VApercent,
                                    decimal productWeight, decimal prdStoneWeight, string productPurity, 
                                    string companyName="MATHA")
    {

        try
        {

            var netWeight = productWeight.ToString("F3");
            var vaPercent = (int)VApercent + "%";
            var stoneWeight = "";
            
            if (prdStoneWeight > 0)
            {
                stoneWeight = prdStoneWeight.ToString("F3");
            }

            string zplCommand = GenerateZPL(productCode, productName, vaPercent, netWeight, stoneWeight,
                                            productPurity, companyName);

            if (RawPrinterHelper.SendZPLToPrinter(PrinterName, zplCommand, out var err))
            {
                return true;
            }

            Console.WriteLine($"Printing barcode for: {err}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Printing barcode for: Error");
        }

        return false;
    }


    private static string GenerateZPL(string productCode, string productName, string VaPercent,
                                    string productWeight, string stoneWeight, string productPurity, string companyName)
    {
        /* return "^XA\n" +
    "^FO50,50\n" +
    "^BY3\n" +
    "^BCN,100,Y,N,N\n" +
    $"^FD{productCode}^FS\n" +
    "^XZ";*/
        string zplCmd = "";

/*        if (firstRec)
            zplCmd = "^XA\n" +

                "^MNA\n" +                              // Non-continuous media
                "^MTT\n" +                              // Thermal Transfer mode
                "^SLC0\n" +                             // Enable auto label detection
                "^JUS\n" +                              // Perform auto-calibration
                "^XA\n" +
                "^PR6\n" +                              // Set print speed (6 = medium speed)
                "^MD30\n" +                             // Set darkness level (30 = medium)
                "^LH0,0\n" +                            // Set label home position
                "^FWN\n";                              // Set print direction (Normal)
        else*/
            zplCmd = "^XA\n";

        zplCmd +=
                "^PW700\n" +
                "^LL100\n" +

                "^FO0,10\n" +                         // Position: X=20, Y=10
                "^BY1\n" +                            // Barcode width
                "^BCN,30,N,N,N\n" +                 // Code 128 Barcode (50 dots high, NO text)"
                $"^FD{productCode}^FS\n" +             // Barcode Data (Replace with actual Product Code)

                // 🔹 Second Line: Product Code
                "^FO0,70\n" +                         // Position below barcode
                "^A0N,18,18\n" +                     // Font size (20 height, 20 width)
                $"^FD{productCode}^FS\n" +                 // Product Code Text

                // 🔹 Second Line: MM/YY
                "^FO140,70\n" +
                "^A0N,18,18\n" +
                $"^FD ^FS\n" +  // MM/YY Text

                "^FO0,90\n" +
                "^A0N,18,18\n" +
                $"^FD{companyName}^FS\n" + // Company Name

                // 🔹 Right Section: Weight and Rate
                "^FO210,10\n" +
                "^A0N,20,20\n" +
                $"^FD{productName}^FS\n" +  // Weight

                "^FO210,30\n" +
                "^A0N,20,20\n" +
                $"^FDGwt: {productWeight}^FS\n";

        //if (stoneWeight is not null )
        if (stoneWeight.Length > 0)
        {
            zplCmd += 
                "^FO210,50\n" +
                "^A0N,20,20\n" +
                $"^FDStone: {stoneWeight}^FS\n";
        }
        
        zplCmd +=
                "^FO210,70\n" +
                "^A0N,20,20\n" +
                $"^FDMC: {VaPercent}^FS\n" +  // Rate

                "^FO210,90\n" +
                "^A0N,20,20\n" +
                $"^FDPurity: {productPurity}^FS\n" +  // Rate

                "^XZ\n";

        return zplCmd;
    }

}

public class BarCodeProductRec
{
    public string productCode { get; set; }
    
}

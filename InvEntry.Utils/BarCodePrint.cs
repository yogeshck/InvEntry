using System.Runtime.Intrinsics.Arm;

namespace InvEntry.Utils;

public class BarCodePrint
{

    private static string PrinterName = "Bar Code Printer TT065-50"; // Set your printer name
                                                                     // private static bool firstRec = true;
    private static bool initialized = false;


    public static bool ProcessBarCode(string productCode, string productName, decimal VApercent,
                                    decimal productWeight, decimal prdStoneWeight, string productPurity,
                                    string companyName = "MATHA")
    {

        try
        {
            // Run initialization once per session
            if (!initialized)
            {
                string initZPL = GenerateInitZPL();
                RawPrinterHelper.SendZPLToPrinter(PrinterName, initZPL, out var initErr);
                initialized = true;
            }


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

    private static string GenerateInitZPL()
    {
        return "^XA\n" +
               "^MNA\n" +   // Non-continuous media
               "^MTT\n" +   // Thermal Transfer mode
               "^SLC0\n" +  // Enable auto label detection
               "^JUS\n" +   // Perform auto-calibration
               "^PR6\n" +   // Print speed
               "^MD30\n" +  // Darkness
               "^LH0,0\n" + // Home position
               "^FWN\n" +   // Print direction
               "^XZ\n";
    }


    private static string GenerateZPL(string productCode, string productName, string VaPercent,
                                    string productWeight, string stoneWeight, string productPurity, string companyName)
    {

        if (companyName.Length > 20)
            companyName = companyName.Substring(0, 19);

        /* return "^XA\n" +
    "^FO50,50\n" +
    "^BY3\n" +
    "^BCN,100,Y,N,N\n" +
    $"^FD{productCode}^FS\n" +
    "^XZ";*/
        string zplCmd = "";

        /*        if (firstRec)
                {
                    firstRec = false;

                    zplCmd = "^XA\n" +

                        "^MNA\n" +                              // Non-continuous media
                        "^MTT\n" +                              // Thermal Transfer mode
                        "^SLC0\n" +                             // Enable auto label detection
                        "^JUS\n" +                              // Perform auto-calibration
                        "^XA\n" +
                        "^PR6\n" +                              // Set print speed (6 = medium speed)
                        "^MD30\n" +                             // Set darkness level (30 = medium)
                        "^LH0,0\n" +                            // Set label home position
                        "^FWN\n"+                              // Set print direction (Normal)
                    "XZ\n";
                }
                else
                {
                    zplCmd = "^XA\n";

                }*/
        // PW = print width - This sets the width of the label (the printable area) to 700 dots.
        // Since most Zebra printers are 203 dpi(dots per inch), 700 dots ≈ 3.45 inches wide.
        // If the printer is 300 dpi, 700 dots ≈ 2.33 inches.

        // LL = Label Length - This sets the length of the label to 250 dots. At 203 dpi, this is about 1.23 inches.
        // At 300 dpi, it's about 0.83 inches.

        // This prints a label 700 dots wide × 250 dots tall

        zplCmd +=
                "^XA\n" +
                "^PW700\n" +                            //700
                "^LL250\n" +                            //250
                "^MD10"+                                // Darkness level (tune between 20–30)  20 to 25

                "^FO5,05\n" +                         // Position: X=20, Y=10 means  x=05 left side of the label
               // prev "^BY1\n" +                            // Barcode width
                "^BY1,1.5,40"+           // Barcode module width=2, ratio=2, height=40

                "^BCN,40,N,N,N\n" +                   // Code 128 Barcode (50 dots high, NO text)"
                $"^FD{productCode}^FS\n" +            // Barcode Data (Replace with actual Product Code)

                // 🔹 Second Line: Product Code
                "^FO5,55\n" +                         // Position below barcode
                "^A0N,20,20\n" +                      // Font size (20 height, 20 width)  prev = A0N,18,25
                $"^FD{productCode}^FS\n" +            // Product Code Text

                // 🔹 Second Line: MM/YY
                "^FO140,55\n" +
                "^A0N,18,18\n" +
                $"^FD ^FS\n" +  // MM/YY Text

               "^FO10,85\n" +
               "^A0N,18,18\n" +
               $"^FD{companyName}^FS\n" + // Company Name

                // 🔹 Right Section: Weight and Rate
                "^FO250,05\n" +              //OLD = 210          //based on the label size perforation position to be shifted ex. -> 210+   
                "^A0N,22,22\n" +                     //prev 20,20
                $"^FD{productName}^FS\n" +             // Weight

                "^FO250,25\n" +                         //OLD = 210
                "^A0N,20,20\n" +
                $"^FDGwt: {productWeight}^FS\n";

        //if (stoneWeight is not null )
        if (stoneWeight.Length > 0)
        {
            zplCmd +=
                "^FO250,45\n" +                         //OLD = 210
                "^A0N,20,20\n" +
                $"^FDStone: {stoneWeight}^FS\n";
        }
        else
        {
            zplCmd +=
                "^FO250,45\n" +                         //OLD = 210
                "^A0N,20,20\n" +
                $"^FD ^FS\n";
        }

        zplCmd +=
                    "^FO250,65\n" +                     //OLD = 210
                    "^A0N,20,20\n" +
                    $"^FDMC: {VaPercent}^FS\n" +  // Rate

                    "^FO250,85\n" +                    //OLD = 210
                    "^A0N,20,20\n" +
                    $"^FDPurity: {productPurity}^FS\n" +  // Rate

                    "^XZ\n";

        return zplCmd;
    }

    public static void ReinitializePrinter()
    {
        try
        {
            string initZPL = GenerateInitZPL();
            RawPrinterHelper.SendZPLToPrinter(PrinterName, initZPL, out var err);
            initialized = true; // reset state
            Console.WriteLine("Printer reinitialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Printer reinit failed: {ex.Message}");
        }
    }
}


public class BarCodeProductRec
{
    public string productCode { get; set; }

}

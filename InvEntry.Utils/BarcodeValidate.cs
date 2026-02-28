using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class BarcodeValidate
    {
        // Regex pattern: 3 uppercase letters, digit, dash, then 6–7 digits
        //below would be implemented once we have standardised barcode
        //private static readonly Regex BarcodeRegex =
        //    new Regex(@"^[A-Z]{3}\d-\d{6,7}$", RegexOptions.Compiled);
        private static readonly Regex BarcodeRegex =
             new Regex(@"^([A-Z]{3}\d-\d{6,7}|[A-Z]{3}-\d-\d{6,7})$", RegexOptions.Compiled);

        public BarcodeValidate() { }

        public string Validate(string barCode)
        {
            if (string.IsNullOrEmpty(barCode))
            {
                return string.Empty;
            }

            barCode = barCode.Trim();

            // Normalize input to uppercase before validation
            string vBarCode = barCode.ToUpperInvariant();

            // Check against regex
            if (BarcodeRegex.IsMatch(vBarCode))
            {
                // Valid barcode → return normalized value
                return vBarCode;
            }
            else
            {
                // Invalid barcode → return empty or throw exception
                return string.Empty;
            }
        }
    }

}

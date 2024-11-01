using DevExpress.Mvvm.Native;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Extension
{
    public static class Tax
    {
        public static void GetSellerLocation()
        {

        }

        public static void GetBuyerLocation()
        {

        }

        public static void GetWithinStateTax(MtblReference gstTaxRefList)
        {
/*            var sgstPerc = gstTaxRefList.RefCode; // (x => x.RefCode.Equals("SGST"));
            var sgstPercent = gstTaxRefList.FirstOrDefault(x => x.RefCode.Equals("SGST"));
            decimal.TryParse(sgstPercent.RefValue.ToString(), out SCGSTPercent);

            if (Buyer?.GstStateCode == "33")
            {
                return SCGSTPercent;//Math.Round(SCGSTPercent / 2, 3);
            }
            return 0M;*/
        }

        public static void GetOutsideStateTax()
        {

        }

    }

}

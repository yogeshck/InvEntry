using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Fields;
using InvEntry.LabelGenerator;
using InvEntry.LabelGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.Helpers
{
    public class LabelGenPrintHelper
    {
        private static bool firstRec = true;

        public static bool GenerateLabel(string prdCode, string prdName, decimal vaPercent,
                                         decimal prdNetWeight, decimal prdStoneWeight,
                                         string productPurity, string companyName = "MATHA")
        {

            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/InvEntry.LabelGenerator;component/Resources/LabelLayout.xaml",UriKind.Absolute)
            };

            var layoutManager = new ZplLayoutManager(dict); // LabelGenerator.App.Current.Resources);

            var values = new Dictionary<string, string>
            {
                { "ProductCode", prdCode },
                { "ProductName", prdName },
                { "VaPercent", vaPercent.ToString("00") },
                { "ProductWeight", prdNetWeight.ToString("0.000") },
                { "StoneWeight", prdStoneWeight.ToString("0.000") },
                { "ProductPurity", productPurity },
                { "CompanyName", companyName }
            };

            string zplCmd = layoutManager.GenerateZPL(values, firstRec);

            // TODO: send zplCmd to printer here
            return true;
        }
    }
}
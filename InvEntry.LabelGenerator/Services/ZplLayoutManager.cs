using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InvEntry.LabelGenerator.Services
{
    public class ZplLayoutManager
    {
        private readonly ResourceDictionary _resources;
        private readonly double _scaleFactor;

        public ZplLayoutManager(ResourceDictionary resources)
        {
            _resources = resources;
            int labelWidth = (int)_resources["LabelWidth"];
            int baseWidth = (int)_resources["BaseWidth"];
            _scaleFactor = (double)labelWidth / baseWidth;
        }

        private int Scale(int value) => (int)(value * _scaleFactor);

        public string GenerateZPL(Dictionary<string, string> values, bool firstRec)
        {
            var sb = new StringBuilder();

            if (firstRec)
            {
                sb.AppendLine("^XA");
                sb.AppendLine("^MNA");
                sb.AppendLine("^MTT");
                sb.AppendLine("^SLC0");
                sb.AppendLine("^JUS");
                sb.AppendLine("^XA");
                sb.AppendLine("^PR6");
                sb.AppendLine("^MD30");
                sb.AppendLine("^LH0,0");
                sb.AppendLine("^FWN");
            }
            else
            {
                sb.AppendLine("^XA");
            }

            sb.AppendLine("^PW700");
            sb.AppendLine("^LL250");

            var fields = (string[])_resources["LabelFields"];
            foreach (var field in fields)
            {
                var parts = field.Split('|');
                string name = parts[0];
                int x = Scale(int.Parse(parts[1]));
                int y = Scale(int.Parse(parts[2]));
                int fontHeight = int.Parse(parts[3]);
                int fontWidth = int.Parse(parts[4]);
                string template = parts[5];

                sb.AppendLine($"^FO{x},{y}");
                if (fontHeight > 0 && fontWidth > 0)
                    sb.AppendLine($"^A0N,{fontHeight},{fontWidth}");

                // Replace placeholders with actual values
                string line = template;
                foreach (var kv in values)
                    line = line.Replace("{" + kv.Key + "}", kv.Value ?? "");

                sb.AppendLine(line);
            }

            sb.AppendLine("^XZ");
            return sb.ToString();
        }
    }
}
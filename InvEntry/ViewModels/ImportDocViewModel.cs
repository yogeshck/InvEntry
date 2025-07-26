using System;
using System.IO;
using System.Linq;
using System.Windows;
using InvEntry.Models;
using Tesseract;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.Xpf.Core;

namespace InvEntry.ViewModels
{
    public partial class ImportDocViewModel : ObservableObject
    {
        [ObservableProperty]
        private InvoiceHeader _Estimate;

        [RelayCommand]
        private void PdfDrop(DragEventArgs args)
        {
            if (!args.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
            string pdfPath = files.FirstOrDefault(x => x.EndsWith(".pdf"));
            if (!string.IsNullOrEmpty(pdfPath))
                ExtractInvoiceFromPdf(pdfPath);
        }

        private void ExtractInvoiceFromPdf(string pdfPath)
        {
            string text = string.Empty;
            using var doc = PdfiumViewer.PdfDocument.Load(pdfPath);
            using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            for (int i = 0; i < doc.PageCount; i++)
            {
                using var bmp = doc.Render(i, 300, 300, true);
                using var stream = new MemoryStream();
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = 0;

                using var img = Pix.LoadFromMemory(stream.ToArray());
                using var page = engine.Process(img);
                text += page.GetText();
            }

            ParseInvoiceText(text);
        }

        private void ParseInvoiceText(string text)
        {
            Estimate = new InvoiceHeader();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("Estimate #"))
                    Estimate.InvNbr = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("DATE"))
                    DateTime.TryParse(line.Split(':').LastOrDefault(), out DateTime dt);
                   // Estimate.InvDate = dt;
                else if (line.Contains("Mr/Ms/Mrs"))
                    Estimate.InvNotes = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("Grand Total"))
                    decimal.TryParse(line.Split().LastOrDefault()?.Replace(",", ""), out var total);
               //     Estimate.GrossRcbAmount = total;
                else if (line.StartsWith("710") || line.StartsWith("711"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 7)
                    {
                        Estimate.Lines.Add(new InvoiceLine
                        {
                            HsnCode = parts[0],
                            ProductName = parts[1],
                            ProductPurity = parts[2],
                            ProdQty = 1 ,  //decimal.Parse(parts[3]),
                            ProdGrossWeight = decimal.Parse(parts[4]),
                            ProdStoneWeight = decimal.Parse(parts[5]),
                            ProdNetWeight = decimal.Parse(parts[6])
                        });  
                    }
                }
            }
        }

        [RelayCommand]
        private void OnReview()
        {
            // Show Review Popup and Confirm Save
            var vm = new ReviewPopupViewModel(Estimate);
        //    var view = new Views.ReviewPopupView { DataContext = vm };
        //    view.ShowDialog();

            if (vm.Confirmed)
            {
                // API call placeholder
                MessageBox.Show("Invoice confirmed. Send to API here.");
            }
        }
    }

}


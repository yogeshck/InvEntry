using DevExpress.Mvvm;
using DevExpress.Pdf;
using Microsoft.Win32;
using PdfiumViewer;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InvEntry.Models;
using Tesseract;
using CommunityToolkit.Mvvm.Input;

namespace InvEntry.ViewModels
{
    public partial class ImportDocViewModel : ViewModelBase
    {
            
        public InvoiceHeader Estimate { get; set; } = new();

        public ICommand DropPdfCommand => new DelegateCommand<DragEventArgs>(OnPdfDropped);
        public ICommand ReviewCommand => new DelegateCommand(OnReview);

        [RelayCommand]
        private void OnPdfDropped(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string pdfPath = files.FirstOrDefault(x => x.EndsWith(".pdf"));
                if (!string.IsNullOrEmpty(pdfPath))
                    ExtractInvoiceFromPdf(pdfPath);
            }
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

            RaisePropertyChanged(() => Estimate);
        }

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


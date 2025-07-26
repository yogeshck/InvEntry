using PdfiumViewer;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using InvEntry.Models;
using Tesseract;
using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.Xpf.Core;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace InvEntry.ViewModels
{
    public partial class ImportDocViewModel : ObservableObject
    {
        [ObservableProperty]
        private EstimateHeader _Estimate;

        [RelayCommand]
        private void PdfDrop(DragEventArgs args)
        {
            if (!args.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
            string pdfPath = files.FirstOrDefault(x => x.EndsWith(".pdf"));
            if (!string.IsNullOrEmpty(pdfPath))
                ExtractInvoiceFromPdf(pdfPath);
        }

        [RelayCommand]
        private void FileImportButton()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Pdf files (*.pdf)|*.pdf|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Select a File to Import",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"Selected file: {filePath}");
                ExtractInvoiceFromPdf(filePath);
            }

        }

        private void ExtractInvoiceFromPdf(string pdfPath)
        {

            string text = TryExtractTextWithDevExpress(pdfPath);

         //  if (string.IsNullOrWhiteSpace(text))
         //   {
         //       text = ExtractTextWithOcr(pdfPath); // Fallback to OCR
         //   }

            ParseInvoiceText(text); // Your existing parsing logic
        }

        private string TryExtractTextWithDevExpress(string pdfPath)
        {
/*            using var processor = new DevExpress.Pdf.PdfDocument();      //PdfDocumentProcessor();
            processor.LoadDocument(pdfPath);

            string fullText = "";
            for (int i = 1; i <= processor.Document.Pages.Count; i++)
            {
                fullText += processor.GetText(i);
            }

            return fullText;*/

            using (var processor = new PdfDocumentProcessor())
            {
                processor.LoadDocument("your.pdf");
                var text = processor.GetText(1);

                return text;
            }
        }

        private void ParseInvoiceText(string text)
        {
            Estimate = new EstimateHeader();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("Estimate #"))
                    Estimate.EstNbr = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("DATE"))
                    DateTime.TryParse(line.Split(':').LastOrDefault(), out DateTime dt);
                   // Estimate.InvDate = dt;
                else if (line.Contains("Mr/Ms/Mrs"))
                    Estimate.EstNotes = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("Grand Total"))
                    decimal.TryParse(line.Split().LastOrDefault()?.Replace(",", ""), out var total);
               //     Estimate.GrossRcbAmount = total;
                else if (line.StartsWith("710") || line.StartsWith("711"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 7)
                    {
                        Estimate.Lines.Add(new EstimateLine
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
                // API call placeholder -- need to implement actual API call logic
                MessageBox.Show("Estimate confirmed. Send to API here.");
            }
        }

    }

}


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghostscript.NET.Rasterizer;
using InvEntry.Models;
using InvEntry.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Tesseract;

namespace InvEntry.ViewModels
{
    public partial class ImportDocViewModel : ObservableObject
    {
        [ObservableProperty]
        private EstimateHeader _estimate;

        [RelayCommand]
        private void PdfDrop(DragEventArgs args)
        {
            if (!args.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])args.Data.GetData(DataFormats.FileDrop);
            string pdfPath = files.FirstOrDefault(x => x.EndsWith(".pdf"));
            if (!string.IsNullOrEmpty(pdfPath))
                ExtractInvoiceWithOcr(pdfPath);
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
                ExtractInvoiceWithOcr(filePath);
            }

        }

        public void ExtractInvoiceWithOcr(string pdfPath)
        {

            var ocrReader = new PdfOcrReader("tessdata");
            string rawText = ocrReader.ExtractTextFromPdf(pdfPath);


           // var images = ConvertPdfToImages(pdfPath);
           // var text = ExtractTextFromImages(images);
           //---- ParseInvoiceText(text);

            var (header, lines) = DocumentParser.ParseDocument<EstimateHeader, EstimateLine>(rawText, "estimate");

            // header and lines are ready to bind or send via API

            Estimate = new EstimateHeader();
            Estimate.EstNbr = header.EstNbr;
            Estimate.EstNotes = header.EstNotes;
            Estimate.EstDate = header.EstDate;
            Estimate.Lines = new ObservableCollection<EstimateLine>(lines);
            //Estimate.Lines.Clear();

        }

        public List<Bitmap> ConvertPdfToImages(string pdfPath)
        {
            var bitmaps = new List<Bitmap>();
            string gsPath = GetGhostscriptPath();

            if (!File.Exists(gsPath))
            {
                MessageBox.Show("Contact Software Support / Admin");
                return bitmaps;
            }

            // Remove: GhostscriptRasterizer.GhostscriptDllPath = gsPath;
            // Instead, use GhostscriptVersionInfo to specify the DLL path
            var version = new Ghostscript.NET.GhostscriptVersionInfo(gsPath);

            using var rasterizer = new GhostscriptRasterizer();
            rasterizer.Open(pdfPath, version, false);

            for (int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
            {
                var img = rasterizer.GetPage(300, pageNumber);
                bitmaps.Add(new Bitmap(img));
            }

            return bitmaps;
        }

        private string GetGhostscriptPath()
        {
            var baseDir = @"C:\\Program Files\\gs";
            if (!Directory.Exists(baseDir)) return string.Empty;

            var latest = Directory.GetDirectories(baseDir)
                                  .OrderByDescending(d => d)
                                  .FirstOrDefault();
            var dllPath = Path.Combine(latest ?? "", "bin", "gsdll64.dll");
            return dllPath;
        }

        public string ExtractTextFromImages(List<Bitmap> images)
        {
            var sb = new StringBuilder();
            
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            foreach (var bmp in images)
            {
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;

                using var img = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(img);
                sb.AppendLine(page.GetText());
            }

            return sb.ToString();
        }

        private void ParseInvoiceText(string text)
        {
            Estimate = new EstimateHeader();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("MONIKAL"))
                    Estimate.EstNotes = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("Estimate #"))
                    Estimate.EstNbr = line.Split(':').LastOrDefault()?.Trim();
                else if (line.Contains("DATE"))
                    DateTime.TryParse(line.Split(':').LastOrDefault(), out DateTime dt);
                   // Estimate.InvDate = dt;
             //   else if (line.Contains("Mr/Ms/Mrs"))
             //       Estimate.EstNotes = line.Split(':').LastOrDefault()?.Trim();
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


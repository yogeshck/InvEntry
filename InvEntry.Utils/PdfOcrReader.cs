using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Ghostscript.NET.Rasterizer;
using Tesseract;

namespace InvEntry.Utils
{
    public class PdfOcrReader
    {
            private readonly string _tessdataPath;

            public PdfOcrReader(string tessdataPath = "tessdata")
            {
                _tessdataPath = tessdataPath;
            }

        private string ExtractTextFromPdf(string pdfPath)
        {
            var text = new StringBuilder();

            using var rasterizer = new GhostscriptRasterizer();
            rasterizer.Open(pdfPath);

            using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

            for (int i = 1; i <= rasterizer.PageCount; i++)
            {
                using var img = rasterizer.GetPage(300, i);
                if (img == null) continue;

                using var stream = new MemoryStream();
                try
                {
                    img.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                    stream.Position = 0;

                    using var pix = Pix.LoadFromMemory(stream.ToArray());
                    using var page = engine.Process(pix);
                    text.AppendLine(page.GetText());
                }
                catch (Exception ex)
                {
                    text.AppendLine($"[Error processing page {i}]: {ex.Message}");
                }
            }

            return text.ToString();
        }




        public async Task<string> ExtractTextAsync(string pdfPath)
        {
            return await Task.Run(() => ExtractTextFromPdf(pdfPath));
        }

        /// <summary>
        /// Extracts text page-by-page (useful for logging or debugging)
        /// </summary>
        public List<string> ExtractPages(string pdfPath)
            {
                var pages = new List<string>();

                using var rasterizer = new GhostscriptRasterizer();
                rasterizer.Open(pdfPath);

                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);

                for (int i = 1; i <= rasterizer.PageCount; i++)
                {
                    using var img = rasterizer.GetPage(300, i);
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    ms.Position = 0;

                    using var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = engine.Process(pix);
                    pages.Add(page.GetText());
                }

                return pages;

            }
        }
 }

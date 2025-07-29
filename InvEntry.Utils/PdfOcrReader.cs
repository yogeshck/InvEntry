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

            /// <summary>
            /// Converts all pages of a PDF to OCR text using Tesseract.
            /// </summary>
            /// <param name="pdfPath">Full path to the PDF</param>
            /// <returns>OCR extracted text</returns>
            public string ExtractTextFromPdf(string pdfPath)
            {
                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException("PDF not found", pdfPath);

                var sb = new StringBuilder();

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
                    sb.AppendLine(page.GetText());
                }

                return sb.ToString();
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

/*

Copyright 2019 Yamborisov Alexey

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using Microsoft.Win32;
using System.IO;

namespace pdfnum
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<string> files;
        public MainWindow()
        {
            files = new ObservableCollection<string>();
            InitializeComponent();
            FilesListView.ItemsSource = files;

            OutPathBox.Text = Properties.Settings.Default.path;
            SearchBox.Text = Properties.Settings.Default.search;
            OffsetH.Text = Properties.Settings.Default.offsetH.ToString();
            OffsetV.Text = Properties.Settings.Default.offsetV.ToString();
            FirstPageInput.Text = Properties.Settings.Default.firstP.ToString();
            NumStartInput.Text = Properties.Settings.Default.numSt.ToString();
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] drop = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (string f in drop)
                {
                    files.Add(f);
                }
            }
        }

        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog folderBrowser = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Выбор папки"
            };
            if (folderBrowser.ShowDialog() == true)
            {
                string pth = System.IO.Path.GetDirectoryName(folderBrowser.FileName);
                OutPathBox.Text = pth;
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Int32.TryParse(FirstPageInput.Text, out int firstPage) && Int32.TryParse(NumStartInput.Text, out int numStart))
            {
                string searchText = SearchBox.Text;
                string destFold = OutPathBox.Text;
                if (destFold[destFold.Length - 1] != '\\')
                {
                    destFold += "\\";
                }
                bool cbSearch = (bool)UserSearchCB.IsChecked;
                float offsetH = 12.6f / 0.3528f;
                float offsetV = 10.6f / 0.3528f;
                if (float.TryParse(OffsetV.Text, out float offV) && float.TryParse(OffsetH.Text, out float offH))
                {
                    offsetH = offH / 0.3528f;
                    offsetV = offV / 0.3528f;
                }
                Parallel.ForEach(files, (f) =>
                {
                    PDF_Test proc = new PDF_Test();
                    if (Directory.Exists(f))
                    {
                        var files = Directory.GetFiles(f, "*.pdf", SearchOption.TopDirectoryOnly).OrderBy(name => name);
                        string tmp_n = destFold + "tmp_" + Thread.CurrentThread.ManagedThreadId.ToString() + ".pdf";
                        proc.MergePdf(files, tmp_n);

                        if (cbSearch)
                        {
                            int fp = 1;

                            foreach (string file in files)
                            {
                                if (file.Contains(searchText))
                                {
                                    break;
                                }
                                PdfReader reader = new PdfReader(file);
                                PdfDocument doc = new PdfDocument(reader);
                                fp += doc.GetNumberOfPages();
                                doc.Close();
                                reader.Close();
                            }
                            proc.NumeratePdf(tmp_n, destFold + Path.GetFileName(f) + ".pdf", fp, numStart, offsetV, offsetH);
                        }
                        else
                        {
                            proc.NumeratePdf(tmp_n, destFold + Path.GetFileName(f) + ".pdf", firstPage, numStart, offsetV, offsetH);
                        }
                        File.Delete(tmp_n);
                    }
                    else if (File.Exists(f))
                    {
                        proc.NumeratePdf(f, destFold + Path.GetFileName(f), firstPage, numStart, offsetV, offsetH);
                    }
                });
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.path = OutPathBox.Text;
            Properties.Settings.Default.search = SearchBox.Text;
            Properties.Settings.Default.offsetH = float.Parse(OffsetH.Text);
            Properties.Settings.Default.offsetV = float.Parse(OffsetV.Text);
            Properties.Settings.Default.firstP = Int32.Parse(FirstPageInput.Text);
            Properties.Settings.Default.numSt = Int32.Parse(NumStartInput.Text);
            Properties.Settings.Default.Save();
        }
    }


    public class PDF_Test
    {
        /*public static void CreateFile(String dest)
        {
            FileInfo file = new FileInfo(dest);
            if (!file.Directory.Exists) file.Directory.Create();
            new PDF_Test().CreatePdf(dest);
        }*/
/*
public static String ReadFile(String dest)
{
    FileInfo file = new FileInfo(dest);
    PdfDocument doc = new PdfDocument(new PdfReader(dest));
    PdfPage page = doc.GetPage(8);
    PdfDictionary pageDict = page.GetPdfObject();
    PdfNumber userUnit = pageDict.GetAsNumber(PdfName.UserUnit);
    int n = doc.GetNumberOfPages();
    for (int i = 1; i <= n; i++)
    {
        PdfPage pg = doc.GetPage(i);
    }
    String result = doc.GetNumberOfPages().ToString() + " num pgs " +
        page.GetPageSize().GetHeight().ToString() + "x" + page.GetPageSize().GetWidth().ToString() + "||" +
        page.GetCropBox().GetHeight().ToString() + "x" + page.GetCropBox().GetWidth().ToString() + "||" +
        page.GetRotation();
    doc.Close();
    return result;
}*/

public void NumeratePdf(string src, string dest, int startP, int startN, float offsetV=36, float offsetH=34)
        {
            FileInfo file = new FileInfo(src);
            PdfDocument pdfDoc = new PdfDocument(new PdfReader(file), new PdfWriter(dest));
            Document doc = new Document(pdfDoc);
            int n = pdfDoc.GetNumberOfPages();
            //float offsetV = 36;
            //float offsetH = 34;
            // Init PdfFont
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
            for (int i = startP; i <= n; i++)
            {
                int angle = pdfDoc.GetPage(i).GetRotation();
                if (angle == 0)
                {
                    doc.ShowTextAligned(new Paragraph((i + startN - startP).ToString()).SetFont(font).SetFontSize(14),
                        pdfDoc.GetPage(i).GetPageSize().GetWidth() - offsetH,
                        pdfDoc.GetPage(i).GetPageSize().GetHeight() - offsetV, i,
                        iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 0);
                } else if (angle == 90)
                {
                    doc.ShowTextAligned(new Paragraph((i + startN - startP).ToString()).SetFont(font).SetFontSize(14),
                        offsetV,
                        pdfDoc.GetPage(i).GetPageSize().GetHeight() - offsetH, i,
                        iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 3.14159265F * 0.5F);
                } else if (angle == 180)
                {
                    doc.ShowTextAligned(new Paragraph((i + startN - startP).ToString()).SetFont(font).SetFontSize(14),
                        offsetH,
                        offsetV, i,
                        iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 3.14159265F);
                } else if (angle == 270)
                {
                    doc.ShowTextAligned(new Paragraph((i + startN - startP).ToString()).SetFont(font).SetFontSize(14),
                        pdfDoc.GetPage(i).GetPageSize().GetWidth() - offsetV,
                        offsetH, i,
                        iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 3.14159265F * 1.5F);
                } else
                {

                }
            }
            doc.Close();
            pdfDoc.Close();
        }

        public static void MergePdf(string pdfSrc1, string pdfSrc2, string pdfDest)
        {
            PdfWriter writer = new PdfWriter(pdfDest);
            PdfDocument pdfDoc = new PdfDocument(writer);
            PdfMerger merger = new PdfMerger(pdfDoc);
            PdfDocument pdfDocSrc = new PdfDocument(new PdfReader(pdfSrc1));
            merger.Merge(pdfDocSrc, 1, pdfDocSrc.GetNumberOfPages());
            pdfDocSrc.Close();
            pdfDocSrc = new PdfDocument(new PdfReader(pdfSrc2));
            merger.Merge(pdfDocSrc, 1, pdfDocSrc.GetNumberOfPages());
            pdfDocSrc.Close();
            merger.Close();
        }
        public void MergePdf(string directory, string pdfDest)
        {
            PdfWriter writer = new PdfWriter(pdfDest);
            PdfDocument pdfDoc = new PdfDocument(writer);
            PdfMerger merger = new PdfMerger(pdfDoc);
            string[] files = Directory.GetFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                PdfDocument pdfDocSrc = new PdfDocument(new PdfReader(file));
                merger.Merge(pdfDocSrc, 1, pdfDocSrc.GetNumberOfPages());
                pdfDocSrc.Close();
            }
            merger.Close();
        }
        public void MergePdf(IEnumerable<string> files, string pdfDest)
        {
            PdfWriter writer = new PdfWriter(pdfDest);
            PdfDocument pdfDoc = new PdfDocument(writer);
            PdfMerger merger = new PdfMerger(pdfDoc);
            foreach (string file in files)
            {
                PdfDocument pdfDocSrc = new PdfDocument(new PdfReader(file));
                merger.Merge(pdfDocSrc, 1, pdfDocSrc.GetNumberOfPages());
                pdfDocSrc.Close();
            }
            merger.Close();
        }
        /*
        public static PdfSize CalcPdf(String dest, bool cb)
        {
            try
            {
                PdfDocument doc = new PdfDocument(new PdfReader(dest));
                int n = doc.GetNumberOfPages();
                int Fold = 0, Formats = 0, A4 = 0, A3 = 0, A2 = 0, A1 = 0, A0 = 0;
                for (int i = 1; i <= n; i++)
                {
                    PdfPage pg = doc.GetPage(i);
                    float pgHeight;
                    float pgWidth;
                    if (cb)
                    {
                        pgHeight = pg.GetCropBox().GetHeight();
                        pgWidth = pg.GetCropBox().GetWidth();
                    }
                    else
                    {
                        pgHeight = pg.GetPageSize().GetHeight();
                        pgWidth = pg.GetPageSize().GetWidth();
                    }
                    if (pgHeight < pgWidth)
                    {
                        float t = pgHeight;
                        pgHeight = pgWidth;
                        pgWidth = t;
                    }

                    int size = (int)Math.Round(pgHeight * pgWidth / 842 / 595 + 0.2F, MidpointRounding.AwayFromZero);
                    if (size > 1)
                    {
                        Fold += 1;
                        Formats += size;
                    }
                    else
                    {
                        Formats += 1;
                    }

                    if ((pgHeight > 2450) || (pgWidth > 1760))
                    {
                        A0 += 1;
                    }
                    else if ((pgHeight > 1760) || (pgWidth > 1250))
                    {
                        A1 += 1;
                    }
                    else if ((pgHeight > 1250) || (pgWidth > 900))
                    {
                        A2 += 1;
                    }
                    else if ((pgHeight > 900) || (pgWidth > 650))
                    {
                        A3 += 1;
                    }
                    else
                    {
                        A4 += 1;
                    }

                }
                PdfSize result = new PdfSize(dest, n, Fold, Formats, A4, A3, A2, A1, A0);
                return result;
            }
            catch (Exception)
            {
                return new PdfSize("Ошибка при обработке файла " + dest);
            }
        }

        public static PdfSize PdfSizeFolder(String directory, bool Flag, bool cb)
        {
            String[] files;
            PdfSize result = new PdfSize(directory);
            if (Flag)
            {
                files = Directory.GetFiles(directory, "*.pdf", SearchOption.AllDirectories);
            }
            else
            {
                files = Directory.GetFiles(directory, "*.pdf", SearchOption.TopDirectoryOnly);
            }
            foreach (String file in files)
            {
                result += CalcPdf(file, cb);
            }
            return result;
        }*/


        /*
        public virtual void CreatePdf(String dest)
        {
            // Init PdfWriter
            PdfWriter writer = new PdfWriter(dest);
            // Init PdfDocument
            PdfDocument pdf = new PdfDocument(writer);
            // Init Document
            Document document = new Document(pdf);
            // Init PdfFont
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
            // Content
            List list = new List().SetSymbolIndent(12).SetListSymbol("\u2022").SetFont(font);
            list.Add(new ListItem("Example"));
            document.Add(new Paragraph("Hello World"));
            document.Add(list);
            // Close
            document.Close();
        }*/
    }

}

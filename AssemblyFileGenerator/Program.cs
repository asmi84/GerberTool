using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GerberParser;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace AssemblyFileGenerator
{
    static class Program
    {
        public class PnPFootprintGroup
        {
            private readonly string _footprintName;
            private readonly string _value;
            private readonly bool _isTopSide;
            private readonly int _count;
            private readonly List<string> _refDes;
            private readonly List<PnPFileEntry> _parts;

            public string FootprintName
            {
                get { return _footprintName; }
            }

            public string Value
            {
                get { return _value; }
            }

            public bool IsTopSide
            {
                get { return _isTopSide; }
            }

            public int Count
            {
                get { return _count; }
            }

            public List<string> RefDes
            {
                get { return _refDes; }
            }

            public List<PnPFileEntry> Parts
            {
                get { return _parts; }
            }

            public PnPFootprintGroup(string footprintName, string value, bool isTopSide, int count, List<string> refDes, List<PnPFileEntry> parts)
            {
                _footprintName = footprintName;
                _value = value;
                _isTopSide = isTopSide;
                _count = count;
                _refDes = refDes;
                _parts = parts;
            }
        }

        private static readonly Regex smd_passive_regex = new Regex("smd_(\\w+)_(\\d{4})", RegexOptions.Compiled);

        class FootprintComparer : IComparer<string>
        {
            private readonly bool _isOrcad = false;

            public FootprintComparer(bool isOrcad)
            {
                _isOrcad = isOrcad;
            }

            public int Compare(string x, string y)
            {
                return _isOrcad ? CompareOrcad(x, y) : CompareKicad(x, y);
            }

            public int CompareKicad(string x, string y)
            {
                var x_smd_passive = x.StartsWith("R_", StringComparison.CurrentCultureIgnoreCase) | x.StartsWith("C_", StringComparison.CurrentCultureIgnoreCase);
                var y_smd_passive = y.StartsWith("R_", StringComparison.CurrentCultureIgnoreCase) | y.StartsWith("C_", StringComparison.CurrentCultureIgnoreCase);
                if (x_smd_passive & !y_smd_passive)
                    return -1;
                if (!x_smd_passive & y_smd_passive)
                    return 1;
                //if (!x_smd_passive && !y_smd_passive)
                return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
            }

            public int CompareOrcad(string x, string y)
            {
                var x_smd_passive = x.StartsWith("smd_res", StringComparison.CurrentCultureIgnoreCase) | x.StartsWith("smd_cap", StringComparison.CurrentCultureIgnoreCase);
                var y_smd_passive = y.StartsWith("smd_res", StringComparison.CurrentCultureIgnoreCase) | y.StartsWith("smd_cap", StringComparison.CurrentCultureIgnoreCase);
                if (x_smd_passive & !y_smd_passive)
                    return -1;
                if (!x_smd_passive & y_smd_passive)
                    return 1;
                //if (!x_smd_passive && !y_smd_passive)
                    return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
                /*var x_m = smd_passive_regex.Match(x);
                var y_m = smd_passive_regex.Match(y);
                if (!x_m.Success || !y_m.Success)
                    return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);*/

            }
        }

        class PnPDataFilter
        {
            private readonly string _refTemplate;
            private readonly bool _isWildcard;

            public PnPDataFilter(string refTemplate)
            {
                _isWildcard = false;
                _refTemplate = refTemplate;
                if (_refTemplate.EndsWith("*"))
                {
                    _isWildcard = true;
                    _refTemplate = _refTemplate.Replace("*", string.Empty);
                }
            }

            public bool Match(string refName)
            {
                if (_isWildcard)
                    return refName.StartsWith(_refTemplate, StringComparison.InvariantCultureIgnoreCase);
                return refName.Equals(_refTemplate, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        static void Main(string[] args)
        {
            var projName = "S7_Min";
            var isOrcad = false;
            for(var i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                    case "-proj":
                        {
                            if (i < args.Length - 1)
                            {
                                projName = args[i + 1];
                                ++i;
                            }
                        }
                        break;
                    case "-orcad":
                        isOrcad = true;
                        break;
                    case "-kicad":
                        isOrcad = false;
                        break;
                }
            }
            var filesFolder = ".\\" + projName + "\\";
            HashSet<string> parts = new HashSet<string>();
            if (File.Exists(filesFolder + "Power_parts.txt"))
                parts = new HashSet<string>(File.ReadAllLines(filesFolder + "Power_parts.txt"));

            var excludeFilter = new List<PnPDataFilter>();
            if (File.Exists(filesFolder + "Ignore.txt"))
            {
                excludeFilter = File.ReadLines(filesFolder + "Ignore.txt").Select(x => new PnPDataFilter(x)).ToList();
            }


            ICADFileParser fileParser;
            if (isOrcad)
                fileParser = new OrcadFileParser();
            else
                fileParser = new KiCADFileParser();

            var pnpFileName = fileParser.GetDefaultPnPFileName(projName);
            if (!File.Exists(Path.Combine(filesFolder, pnpFileName)))
                if (string.IsNullOrEmpty(pnpFileName))
                {
                    Console.WriteLine("ERROR - PNP file is not found! Exiting...");
                    return;
                }

            var pnpData = fileParser.ParseFile(Path.Combine(filesFolder, pnpFileName));

            pnpData = pnpData.Where(x => excludeFilter.All(l => !l.Match(x.RefDes)))
                .ToList();

            /*pnpData = pnpData.Where(x => parts.Contains(x.RefDes))
                .ToList();*/

            var bomFileName = Path.Combine(filesFolder, fileParser.GetDefaultBoMFileName(projName));
            //var bomFileName = Directory.GetFiles(filesFolder, "*.BOM").FirstOrDefault();
            if (string.IsNullOrEmpty(bomFileName))
            {
                Console.WriteLine("ERROR - BOM file is not found! Exiting...");
                return;
            }

            var bomData = fileParser.LoadValuesFromBOMFile(bomFileName);
            foreach (var pnPFileEntry in pnpData)
            {
                pnPFileEntry.Value = bomData[pnPFileEntry.RefDes];
            }
            var bom = pnpData.GroupBy(x => new { x.FootprintName, x.Value })
                .Select(x => new { x.Key.FootprintName, x.Key.Value, Count = x.Count() })
                .ToList();
            File.WriteAllLines(filesFolder + "bom.csv", bom.Select(x => string.Join(",", new string[] { x.FootprintName, x.Value, x.Count.ToString() })));

            var res = pnpData.GroupBy(x => new { x.FootprintName, x.Value, x.IsTopSide })
                .Select(x => new PnPFootprintGroup(x.Key.FootprintName, x.Key.Value, x.Key.IsTopSide, x.Count(), x.Select(_ => _.RefDes).ToList(), x.ToList()))
                .OrderByDescending(x => x.Count).ThenBy(x => x.FootprintName, new FootprintComparer(isOrcad))
                .ToList();

            var topCopperFileName = Path.Combine(filesFolder, fileParser.GetCopperName(projName, true));
            if (!File.Exists(topCopperFileName))
            {
                Console.WriteLine($"ERROR - top copper file {topCopperFileName} is not found. Exiting...");
                return;
            }
            var topSilkFileName = Path.Combine(filesFolder, fileParser.GetSilkName(projName, true));
            if (!File.Exists(topSilkFileName))
            {
                Console.WriteLine($"ERROR - top silkscreen file {topSilkFileName} is not found. Exiting...");
                return;
            }

            var bottomCopperFileName = Path.Combine(filesFolder, fileParser.GetCopperName(projName, false));
            if (!File.Exists(bottomCopperFileName))
            {
                Console.WriteLine($"ERROR - bottom copper file {bottomCopperFileName} is not found. Exiting...");
                return;
            }
            var bottomSilkFileName = Path.Combine(filesFolder, fileParser.GetSilkName(projName, false));
            if (!File.Exists(bottomSilkFileName))
            {
                Console.WriteLine($"ERROR - bottom silkscreen file {bottomSilkFileName} is not found. Exiting...");
                return;
            }

            CreateSideImage(pnpData.Where(x => x.IsTopSide).ToList(), topCopperFileName, topSilkFileName, false);
            CreateSideImage(pnpData.Where(x => !x.IsTopSide).ToList(), bottomCopperFileName, bottomSilkFileName, true);

            PdfDocument doc = new PdfDocument();
                
            Console.WriteLine("Creating pages for top side...");
            CreatePages(doc, res.Where(x => x.IsTopSide).ToList(), topCopperFileName, topSilkFileName, false);
            Console.WriteLine("Creating pages for bottom side...");
            CreatePages(doc, res.Where(x => !x.IsTopSide).ToList(), bottomCopperFileName, bottomSilkFileName, true);

            doc.Save(filesFolder + "Out.pdf");

            Process.Start(filesFolder + "Out.pdf");
        }

        private static void CreateSideImage(IList<PnPFileEntry> parts, string copperName, string silkName, bool isBottom)
        {
            decimal scaleX, scaleY, offsetX, offsetY;
            var combinedImg = CreateSideImage(copperName, silkName, true, out scaleX, out scaleY, out offsetX, out offsetY);
            var gx = Graphics.FromImage(combinedImg);
            foreach (var part in parts)
            {
                var partX = (int)((part.X - offsetX) * scaleX);
                var partY = (int)(combinedImg.Height - ((part.Y - offsetY) * scaleY));
                //var rect = new Rectangle(partX - 5, partY - 5, 10, 10);
                var rect = new Rectangle(-15, -10, 30, 20);
                if (part.FootprintName.Contains("0201"))
                    rect = new Rectangle(-5, -3, 10, 6);
                else if (part.FootprintName.Contains("0402"))
                    rect = new Rectangle(-8, -5, 16, 10);

                var mat = gx.Transform;
                gx.TranslateTransform(partX, partY);
                gx.RotateTransform(part.Rotation);
                gx.FillRectangle(Brushes.Red, rect);
                gx.Transform = mat;
            }
            gx.Dispose();
            if (isBottom)
                combinedImg.RotateFlip(RotateFlipType.RotateNoneFlipX);
            combinedImg.Save(isBottom ? "Bottom.bmp" : "Top.bmp");
        }

        private static void CreatePages(PdfDocument doc, IList<PnPFootprintGroup> parts, string copperName, string silkName, bool isBottom)
        {
            decimal scaleX, scaleY, offsetX, offsetY;
            var combinedImg = CreateSideImage(copperName, silkName, false, out scaleX, out scaleY, out offsetX, out offsetY);
            
            const int partsPerPage = 5;

            var pagesCount = parts.Count / partsPerPage;
            if (parts.Count > pagesCount * partsPerPage)
                pagesCount++;

            const int colorX = 10;
            const int footprintX = 40;
            const int valueX = 200;
            const int countX = 310;
            const int refdesX = 350;

            var brushes = new XBrush[]
            {
                XBrushes.Red,
                XBrushes.Yellow,
                XBrushes.DarkGreen,
                XBrushes.Aqua,
                XBrushes.Purple,
                XBrushes.Orange,
                XBrushes.DarkBlue,
                XBrushes.SaddleBrown,
                XBrushes.Fuchsia,
                XBrushes.Lime,
                XBrushes.SteelBlue,
                XBrushes.Black
            };

            var colors = new Brush[]
            {
                Brushes.Red,
                Brushes.Yellow,
                Brushes.DarkGreen,
                Brushes.Aqua,
                Brushes.Purple,
                Brushes.Orange,
                Brushes.DarkBlue,
                Brushes.SaddleBrown,
                Brushes.Fuchsia,
                Brushes.Lime,
                Brushes.SteelBlue,
                Brushes.Black
            };

            XFont font = new XFont("Consolas", 10, XFontStyle.Regular);
            PdfOutline outline = null;

            var currPage = 1;

            var currPart = 0;

            while (currPart < parts.Count)
            {
                Console.WriteLine($"Creating page {currPage}...");
                var currImg = combinedImg.Clone(new Rectangle(0, 0, combinedImg.Width, combinedImg.Height), combinedImg.PixelFormat);
                var page = doc.AddPage();

                if (outline == null)
                    outline = doc.Outlines.Add(isBottom ? "Bottom side" : "Top side", page, true);
                outline.Outlines.Add($"Page {currPage}", page, true);

                var gfx = XGraphics.FromPdfPage(page);

                var currY = 15;

                gfx.DrawString("Color", font, XBrushes.Black, new XPoint(colorX, currY));
                gfx.DrawString("Footprint", font, XBrushes.Black, new XPoint(footprintX, currY));
                gfx.DrawString("Value", font, XBrushes.Black, new XPoint(valueX, currY));
                gfx.DrawString("Count", font, XBrushes.Black, new XPoint(countX, currY));
                gfx.DrawString("RefDes", font, XBrushes.Black, new XPoint(refdesX, currY));
                var currIdx = 0;
                while (currY < 220 && currIdx < brushes.Length && currPart < parts.Count)
                {
                    currY += 15;
                    gfx.DrawRectangle(XPens.Black, brushes[currIdx], new Rectangle(colorX, currY - 7, 25, 7));

                    gfx.DrawString(parts[currPart].FootprintName.Ellipsis(27), font, XBrushes.Black, new XPoint(footprintX, currY));
                    gfx.DrawString(parts[currPart].Value.Ellipsis(19), font, XBrushes.Black, new XPoint(valueX, currY));
                    gfx.DrawString(parts[currPart].Count.ToString(), font, XBrushes.Black, new XPoint(countX, currY));
                    const int partsPerLine = 9;
                    if (parts[currPart].RefDes.Count <= partsPerLine)
                        gfx.DrawString(string.Join(",", parts[currPart].RefDes), font, XBrushes.Black, new XPoint(refdesX, currY));
                    else
                    {
                        var linesCount = parts[currPart].RefDes.Count / partsPerLine;
                        if (parts[currPart].RefDes.Count > linesCount * partsPerLine)
                            linesCount++;
                        for (int k = 0; k < linesCount; k++)
                        {
                            var isLastLine = k == linesCount - 1;
                            gfx.DrawString(string.Join(",", parts[currPart].RefDes.Skip(k * partsPerLine).Take(partsPerLine)) + ((!isLastLine) ? "," : ""), 
                                font, XBrushes.Black, new XPoint(refdesX, currY));
                            if (!isLastLine)
                                currY += 15;
                        }
                    }

                    var brush = colors[currIdx];
                    var gx = Graphics.FromImage(currImg);
                    foreach (var part in parts[currPart].Parts)
                    {
                        var partX = (int)((part.X - offsetX) * scaleX);
                        var partY = (int)(combinedImg.Height - ((part.Y - offsetY) * scaleY));
                        //var rect = new Rectangle(partX - 5, partY - 5, 10, 10);
                        var rect = new Rectangle(-15, -10, 30, 20);
                        if (part.FootprintName.Contains("0201"))
                            rect = new Rectangle(-5, -3, 10, 6);
                        else if (part.FootprintName.Contains("0402"))
                            rect = new Rectangle(-8, -5, 16, 10);

                        var mat = gx.Transform;
                        gx.TranslateTransform(partX, partY);
                        gx.RotateTransform(part.Rotation);
                        gx.FillRectangle(brush, rect);
                        gx.Transform = mat;
                    }
                    gx.Dispose();
                    currIdx++;
                    currPart++;
                }
                Console.WriteLine($"Max currY = {currY}");
                if (isBottom)
                    currImg.RotateFlip(RotateFlipType.RotateNoneFlipX);
                var ximgCombinedImg = XImage.FromGdiPlusImage(currImg);

                gfx.DrawImage(ximgCombinedImg, new Rectangle(10, 250, 590, 540));
                //break;
                currPage++;
            }
        }

        private static string Ellipsis(this string str, int limit)
        {
            if (str.Length > limit)
                return str.Substring(0, limit - 3) + "...";
            return str;
        }

        private static Bitmap CreateSideImage(string copperName, string silkName, bool isWriteBg,
            out decimal scaleX, out decimal scaleY, out decimal offsetX, out decimal offsetY)
        {
            var topCopperFile = new GerberFileObject();
            topCopperFile.ReadFile(copperName);
            decimal minX_topCopper, maxX_topCopper, minY_topCopper, maxY_topCopper;
            GerberFileProcessor.CalculateExtents(topCopperFile, out minX_topCopper, out maxX_topCopper, out minY_topCopper, out maxY_topCopper);
            var topSilkFile = new GerberFileObject();
            topSilkFile.ReadFile(silkName);
            decimal minX_topSilk, maxX_topSilk, minY_topSilk, maxY_topSilk;
            GerberFileProcessor.CalculateExtents(topSilkFile, out minX_topSilk, out maxX_topSilk, out minY_topSilk, out maxY_topSilk);
            decimal minX, maxX, minY, maxY;
            minX = Math.Min(minX_topCopper, minX_topSilk);
            maxX = Math.Max(maxX_topCopper, maxX_topSilk);
            minY = Math.Min(minY_topCopper, minY_topSilk);
            maxY = Math.Max(maxY_topCopper, maxY_topSilk);

            var sizeX = maxX - minX;
            var sizeY = maxY - minY;
            offsetX = minX;
            offsetY = minY;

            var topCopper = new GerberFileRender(Color.FromArgb(255, 128, 128, 128), isWriteBg ? Color.White : Color.FromArgb(0, 0, 0, 0))
                .CreateImageBitmap(topCopperFile, 20, minX, maxX, minY, maxY);
            var topSilk = new GerberFileRender(Color.FromArgb(255, 0, 0, 255), isWriteBg ? Color.White : Color.FromArgb(0, 0, 0, 0))
                .CreateImageBitmap(topSilkFile, 20, minX, maxX, minY, maxY);

            var combinedImg = new Bitmap(topCopper.Width, topCopper.Height);

            scaleX = combinedImg.Width / sizeX;
            scaleY = combinedImg.Height / sizeY;

            var fullRect = new Rectangle(0, 0, topCopper.Width, topCopper.Height);
            var gx = Graphics.FromImage(combinedImg);

            float[][] matrixAlpha =
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 0.5f, 0},
                new float[] {0, 0, 0, 0, 1}
            };
            ColorMatrix colorMatrix = new ColorMatrix(matrixAlpha);

            ImageAttributes iaAlphaBlend = new ImageAttributes();
            iaAlphaBlend.SetColorMatrix(
             colorMatrix,
             ColorMatrixFlag.Default,
             ColorAdjustType.Bitmap);
            gx.DrawImage(topCopper, fullRect, 0, 0, fullRect.Width, fullRect.Height, GraphicsUnit.Pixel, iaAlphaBlend);
            gx.DrawImage(topSilk, fullRect, 0, 0, fullRect.Width, fullRect.Height, GraphicsUnit.Pixel, iaAlphaBlend);
            gx.Dispose();
            return combinedImg;
        }
    }
}

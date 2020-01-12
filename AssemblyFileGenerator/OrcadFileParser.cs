using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyFileGenerator
{
    public class OrcadFileParser : ICADFileParser
    {
        public IList<PnPFileEntry> ParseFile(string fileName)
        {
            var result = new List<PnPFileEntry>();
            foreach (var line in File.ReadAllLines(fileName))
            {
                //skip headers and comments
                if (!line.Contains("!") || line.StartsWith("#"))
                    continue;
                var parts = line.Split(new[] {'!'}, 7, StringSplitOptions.None);
                if (parts.Length != 7)
                    continue;
                result.Add(new PnPFileEntry
                {
                    RefDes = parts[0].Trim(),
                    X = decimal.Parse(parts[1].Trim()),
                    Y = decimal.Parse(parts[2].Trim()),
                    Rotation = int.Parse(parts[3].Trim()),
                    IsTopSide = parts[4].Trim().ToLower() != "m",
                    FootprintName = parts[5].Trim(),
                });
            }
            return result;
        }

        public IDictionary<string, string> LoadValuesFromBOMFile(string fileName)
        {
            var result = new Dictionary<string, string>();
            var refDesIdx = -1;
            var valueIdx = -1;
            string value = string.Empty;
            foreach (var line in File.ReadAllLines(fileName))
            {
                //skip headers and comments
                if (!line.Contains("\t"))
                    continue;
                if (line.StartsWith("Bill Of")) // header
                    continue;
                var parts = line.Split('\t');
                if (refDesIdx == -1 || valueIdx == -1)
                {
                    //header line is the first "data" line
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i] == "Reference")
                            refDesIdx = i;
                        else if (parts[i] == "Part")
                            valueIdx = i;
                    }
                }
                else
                {
                    var refDesList = parts[refDesIdx].Split(new []{ ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > valueIdx)
                        value = parts[valueIdx];
                    foreach (var refDes in refDesList)
                    {
                        result.Add(refDes, value);
                    }
                }
            }
            return result;
        }

        public string GetDefaultPnPFileName(string projName)
        {
            return "place_txt.txt";
        }

        public string GetDefaultBoMFileName(string projName)
        {
            return projName + ".BOM";
        }

        public string GetSilkName(string projName, bool isTop)
        {
            return isTop ? "SILKSCREEN_TOP.art" : "SILKSCREEN_BOTTOM.art";
        }

        public string GetCopperName(string projName, bool isTop)
        {
            return isTop ? "TOP.art" : "BOTTOM.art";
        }
    }
}
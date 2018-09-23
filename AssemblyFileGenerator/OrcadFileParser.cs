using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyFileGenerator
{
    public class OrcadFileParser
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
    }

    public class PnPFileEntry
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }

        public int Rotation { get; set; }
        public bool IsTopSide { get; set; }
        public string FootprintName { get; set; }
        public string RefDes { get; set; }
        public string Value { get; set; }
    }
}
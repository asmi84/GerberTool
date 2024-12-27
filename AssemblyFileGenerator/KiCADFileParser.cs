using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyFileGenerator
{
    public class KiCADFileParser : ICADFileParser
    {
        public IDictionary<string, string> LoadValuesFromBOMFile(string fileName)
        {
            var result = new Dictionary<string, string>();
            var refDesIdx = -1;
            var valueIdx = -1;
            string value = string.Empty;
            foreach (var line in File.ReadAllLines(fileName))
            {
                var parts = line.Split(';');
                if (refDesIdx == -1 || valueIdx == -1)
                {
                    //header line is the first "data" line
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Trim('"') == "Designator")
                            refDesIdx = i;
                        else if (parts[i].Trim('"') == "Designation")
                            valueIdx = i;
                    }
                }
                else
                {
                    var refDesList = parts[refDesIdx].Trim('"').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > valueIdx)
                        value = parts[valueIdx].Trim('"');
                    foreach (var refDes in refDesList)
                    {
                        if (refDes == "REF**")
                            continue;
                        result.Add(refDes, value);
                    }
                }
            }
            return result;
        }

        public IList<PnPFileEntry> ParseFile(string fileName)
        {
            var result = new List<PnPFileEntry>();
            foreach (var line in File.ReadAllLines(fileName))
            {
                if (line.StartsWith("#"))
                    continue;
                var parts = line.Split(new[] { ' ' }, 7, StringSplitOptions.RemoveEmptyEntries);
                var entry = new PnPFileEntry
                {
                    FootprintName = parts[2],
                    IsTopSide = parts[6] == "top",
                    RefDes = parts[0],
                    Rotation = (int)decimal.Parse(parts[5]),
                    Value = parts[1].Replace("_", " "),
                    X = decimal.Parse(parts[3]),
                    Y = decimal.Parse(parts[4])
                };
                if (!entry.IsTopSide)
                {
                    entry.X = -entry.X;
                }
                result.Add(entry);
            }
            return result;
        }

        public string GetDefaultPnPFileName(string projName)
        {
            return projName + "-all.pos";
        }

        public string GetDefaultBoMFileName(string projName)
        {
            return projName + ".csv";
        }

        public string GetSilkName(string projName, bool isTop)
        {
            var fileName =  isTop ? projName + "-F_SilkS.gto" : projName + "-B_SilkS.gbo";
            if (!File.Exists(Path.Combine(projName, fileName)))
                fileName = isTop ? projName + "-F_SilkS.gbr" : projName + "-B_SilkS.gbr";
            return fileName;
        }

        public string GetCopperName(string projName, bool isTop)
        {
            var fileName = isTop ? projName + "-F_Cu.gtl" : projName + "-B_Cu.gbl";
            if (!File.Exists(Path.Combine(projName, fileName)))
                fileName = isTop ? projName + "-F_Cu.gbr" : projName + "-B_Cu.gbr";
            return fileName;
        }
    }
}
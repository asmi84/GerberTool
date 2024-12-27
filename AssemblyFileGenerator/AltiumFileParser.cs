using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssemblyFileGenerator
{
    public class AltiumFileParser : ICADFileParser
    {
        private readonly IDictionary<string, string> _footprintAliases = new Dictionary<string, string>();

        private void LoadFootprintAliases(string fileName)
        {
            _footprintAliases.Clear();
            if (File.Exists(fileName))
            {
                foreach (var line in File.ReadAllLines(fileName))
                {
                    var parts = line.Split(new[] { ':' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;
                    var fpName = parts[0].Trim();
                    var aliases = parts[1].Trim().Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (string.IsNullOrEmpty(fpName) || aliases.Length == 0)
                        continue;
                    foreach (var alias in aliases)
                    {
                        _footprintAliases[alias.Trim()] = fpName;
                    }
                }
            }
        }
        public string GetCopperName(string projName, bool isTop)
        {
            var files = Directory.GetFiles(projName, isTop ? "*.gtl" : "*.gbl");
            return Path.GetFileName(files.First());
        }

        public string GetDefaultBoMFileName(string projName)
        {
            return $"{projName}.csv";
        }

        public string GetDefaultPnPFileName(string projName)
        {
            var files = Directory.GetFiles(projName, "Pick Place*.csv");
            return Path.GetFileName(files.First());
        }

        public string GetSilkName(string projName, bool isTop)
        {
            var files = Directory.GetFiles(projName, isTop ? "*.gto" : "*.gbo");
            return Path.GetFileName(files.First());
        }

        public IDictionary<string, string> LoadValuesFromBOMFile(string fileName)
        {
            var result = new Dictionary<string, string>();
            var refDesIdx = -1;
            var valueIdx = -1;
            var partNumberIdx = -1;
            string value = string.Empty;
            foreach (var line in File.ReadAllLines(fileName))
            {
                string[] parts;
                if (refDesIdx == -1)
                {
                    //first line
                    parts = line.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    /*refDesIdx = line.IndexOf("Designator");
                    valueIdx = line.IndexOf("Value");*/
                    for (var i = 0; i < parts.Length; i++)
                    {
                        switch (parts[i])
                        {
                            case "Designator":
                                refDesIdx = i;
                                break;
                            case "Value":
                                valueIdx = i;
                                break;
                            case "PartNumber":
                            case "Part Number":
                                partNumberIdx = i;
                                break;
                        }
                    }
                    continue;
                }
                if (!line.Contains(","))
                    continue;
                parts = line.Split(new[] { "\"," }, System.StringSplitOptions.RemoveEmptyEntries);
                var refs = parts[refDesIdx].Trim('"');
                var val = parts[valueIdx].Trim('"');
                var pn = partNumberIdx != -1 ? parts[partNumberIdx].Trim('"') : val;
                foreach (var refdes in refs.Split(','))
                {
                    result.Add(refdes.Trim(), string.IsNullOrEmpty(val) ? pn : val);
                }
            }
            return result;
        }

        public IList<PnPFileEntry> ParseFile(string fileName)
        {
            var path = Path.GetDirectoryName(fileName);
            var aliasFile = Path.Combine(path, "footprint_groups.txt");
            LoadFootprintAliases(aliasFile);
            var result = new List<PnPFileEntry>();
            var refdefIdx = -1;
            var layerIdx = -1;
            var footprintIdx = -1;
            var centerXIdx = -1;
            var centerYIdx = -1;
            var rotationIdx = -1;
            foreach (var line in File.ReadAllLines(fileName))
            {
                if (!line.StartsWith("\""))
                    continue;
                var parts = line.Split(new[] { "\"," }, System.StringSplitOptions.RemoveEmptyEntries);
                if (refdefIdx == -1)
                {
                    //first line
                    for(var i = 0; i < parts.Length; i++)
                    {
                        switch (parts[i].Trim('"'))
                        {
                            case "Designator":
                                refdefIdx = i;
                                break;
                            case "Layer":
                                layerIdx = i;
                                break;
                            case "Footprint":
                                footprintIdx = i;
                                break;
                            case "Center-X(mm)":
                                centerXIdx = i;
                                break;
                            case "Center-Y(mm)":
                                centerYIdx = i;
                                break;
                            case "Rotation":
                                rotationIdx = i;
                                break;
                        }
                    }
                    continue;
                }
                var entry = new PnPFileEntry
                {
                    FootprintName = parts[footprintIdx].Trim('"'),
                    IsTopSide = parts[layerIdx].Trim('"') == "TopLayer",
                    RefDes = parts[refdefIdx].Trim('"'),
                    Rotation = (int)decimal.Parse(parts[rotationIdx].Trim('"')),
                    //Value = parts[1].Replace("_", " "),
                    X = decimal.Parse(parts[centerXIdx].Trim('"')),
                    Y = decimal.Parse(parts[centerYIdx].Trim('"'))
                };
                if (_footprintAliases.ContainsKey(entry.FootprintName) )
                {
                    entry.FootprintName = _footprintAliases[entry.FootprintName];
                }
                /*if (!entry.IsTopSide)
                {
                    entry.X = -entry.X;
                }*/
                result.Add(entry);
            }
            return result;
        }
    }
}

using System.Collections.Generic;

namespace AssemblyFileGenerator
{
    public interface ICADFileParser
    {
        IDictionary<string, string> LoadValuesFromBOMFile(string fileName);
        IList<PnPFileEntry> ParseFile(string fileName);
        string GetDefaultPnPFileName(string projName);
        string GetDefaultBoMFileName(string projName);

        string GetSilkName(string projName, bool isTop);
        string GetCopperName(string projName, bool isTop);
    }
}
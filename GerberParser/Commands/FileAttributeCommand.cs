using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("FileAttribute Name={AttrName} Values={DebuggerDisplay}")]
    public class FileAttributeCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "TF";

        private static string GetNextAttrToken(ref string text)
        {
            var idx = 0;
            while (idx < text.Length && text[idx] != ',')
            {
                idx++;
            }
            var str = text.Substring(0, idx);
            text = text.Substring(idx < text.Length ? idx + 1 : idx);
            return str;
        }

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            var cmd = new FileAttributeCommand();
            cmd.AttrName = GetNextAttrToken(ref text);
            while (!string.IsNullOrWhiteSpace(text))
            {
                var attVal = GetNextAttrToken(ref text);
                cmd.AttrValues.Add(attVal);
            }
            return cmd;
        }

        private string DebuggerDisplay => string.Join(",", AttrValues);

        public string AttrName { get; set; }
        public IList<string> AttrValues { get; set; } = new List<string>();

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            var str = $"%{COMMAND_CODE}{AttrName}";
            if (AttrValues.Any())
                str += "," + string.Join(",", AttrValues);
            str += "*%";
            return str;
        }
    }
}
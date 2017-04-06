using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace GerberParser.Commands
{
    [DebuggerDisplay("FormatStatement Int={IntegerPositions}, Dec={DecimalPositions}")]
    public class FormatStatementCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "FS";
        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            var regex = new Regex("X(\\d\\d)Y(\\d\\d)");
            var res = regex.Match(text);
            var pos = res.Groups[1].Value;
            var intPos = int.Parse(pos[0].ToString());
            var decPos = int.Parse(pos[1].ToString());
            return new FormatStatementCommand
            {
                DecimalPositions = decPos,
                IntegerPositions = intPos
            };
        }

        public int IntegerPositions { get; set; }
        public int DecimalPositions { get; set; }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return string.Format("%FSLAX{0}{1}Y{0}{1}*%", IntegerPositions, DecimalPositions);
        }
        public static GerberCommand Init(int intPos, int decPos)
        {
            return new FormatStatementCommand { IntegerPositions = intPos, DecimalPositions = decPos, CommandCode = COMMAND_CODE };
        }
    }
}
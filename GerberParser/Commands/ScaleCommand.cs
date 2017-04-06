using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Scale Factor={Factor}")]
    public class ScaleCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "LS";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            return new ScaleCommand
            {
                Factor = decimal.Parse(text)
            };
        }

        public decimal Factor { get; private set; }
    }
}
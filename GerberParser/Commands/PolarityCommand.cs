using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Polarity IsDark={IsDark}")]
    public class PolarityCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "LP";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            var isDark = text == "D";
            return new PolarityCommand { IsDark = isDark };
        }

        public bool IsDark { get; private set; }
    }
}
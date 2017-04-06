using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Rotation Angle={Angle}")]
    public class RotationCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "LR";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            return new RotationCommand
            {
                Angle = decimal.Parse(text)
            };
        }

        public decimal Angle { get; private set; }
    }
}
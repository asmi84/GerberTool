using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Mirror X={MirrorX}, Y={MirrorY}")]
    public class MirrorCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "LM";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            return new MirrorCommand
            {
                MirrorX = text.Contains("X"),
                MirrorY = text.Contains("Y"),
            };
        }

        public bool MirrorX { get; private set; }
        public bool MirrorY { get; private set; }
    }
}
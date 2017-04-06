using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("RegionBegin")]
    public class RegionBeginCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G36";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new RegionBeginCommand();
        }
    }
}
using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("RegionEnd")]
    public class RegionEndCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G37";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new RegionEndCommand();
        }
    }
}
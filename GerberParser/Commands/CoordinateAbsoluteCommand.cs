using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("CoordinateAbsolute")]
    public class CoordinateAbsoluteCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G90";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new CoordinateAbsoluteCommand();
        }

        public override bool IsObsolete()
        {
            return true;
        }
    }
}
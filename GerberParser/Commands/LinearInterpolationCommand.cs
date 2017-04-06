using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("LinearInterpolation")]
    public class LinearInterpolationCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G01";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new LinearInterpolationCommand();
        }
    }
}
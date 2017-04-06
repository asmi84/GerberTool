using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("CoordinateIncremental")]
    public class CoordinateIncrementalCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G91";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new CoordinateIncrementalCommand();
        }

        public override bool IsObsolete()
        {
            return true;
        }
    }
}
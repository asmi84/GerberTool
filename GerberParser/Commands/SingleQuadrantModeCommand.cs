using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("SingleQuadrantMode")]
    public class SingleQuadrantModeCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G74";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new SingleQuadrantModeCommand();
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return COMMAND_CODE + "*";
        }
    }
}
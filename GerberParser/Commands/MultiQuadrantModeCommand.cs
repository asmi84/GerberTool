using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("MultiQuadrantMode")]
    public class MultiQuadrantModeCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G75";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new MultiQuadrantModeCommand();
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return COMMAND_CODE + "*";
        }
    }
}
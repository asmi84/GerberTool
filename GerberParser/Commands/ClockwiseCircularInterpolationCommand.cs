using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("ClockwiseCircularInterpolation")]
    public class ClockwiseCircularInterpolationCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G02";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new ClockwiseCircularInterpolationCommand();
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return COMMAND_CODE + "*";
        }
    }
}
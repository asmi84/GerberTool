using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("CounterclockwiseCircularInterpolatio")]
    public class CounterclockwiseCircularInterpolationCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G03";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new CounterclockwiseCircularInterpolationCommand();
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return COMMAND_CODE + "*";
        }
    }
}
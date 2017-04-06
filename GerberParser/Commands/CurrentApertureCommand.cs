using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("CurrentAperture Number={Number}")]
    public class CurrentApertureCommand : GerberFunctionCodeCommand
    {
        public int Number { get; set; }
        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return string.Format("D{0}*", Number);
        }
    }
}
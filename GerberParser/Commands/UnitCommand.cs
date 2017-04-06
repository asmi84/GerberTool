using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Unit IsMetric={IsMetric}")]
    public class UnitCommand : GerberExtendedCommand
    {
        public const string COMMAND_CODE = "MO";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            var isMetric = text == "MM";
            return new UnitCommand {IsMetric = isMetric};
        }

        public bool IsMetric { get; set; }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return string.Format("%MO{0}*%", IsMetric ? "MM" : "IN");
        }

        public static GerberCommand Init(bool isMetric)
        {
            return new UnitCommand {IsMetric = true, CommandCode = COMMAND_CODE};
        }
    }
}
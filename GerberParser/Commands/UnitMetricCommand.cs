using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("UnitMetric")]
    public class UnitMetricCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G71";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new UnitMetricCommand();
        }

        public override bool IsObsolete()
        {
            return true;
        }
    }
}
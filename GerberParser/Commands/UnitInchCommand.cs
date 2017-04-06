using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("UnitInch")]
    public class UnitInchCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G70";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new UnitInchCommand();
        }

        public override bool IsObsolete()
        {
            return true;
        }
    }
}
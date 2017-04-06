using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("FileEnd")]
    public class FileEndCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "M02";

        public static GerberFunctionCodeCommand Create(string data)
        {
            return new FileEndCommand();
        }
    }
}
using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Comment {Comment}")]
    public class CommendCommand : GerberFunctionCodeCommand
    {
        public const string COMMAND_CODE = "G04";
        public string Comment { get; private set; }

        public static GerberFunctionCodeCommand Create(string data)
        {
            var comment = data.Substring(3).Trim();
            return new CommendCommand {Comment = comment};
        }
    }
}
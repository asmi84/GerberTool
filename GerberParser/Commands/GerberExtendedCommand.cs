using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Extended Code={CommandCode}, Blocks={BlocksString}")]
    public class GerberExtendedCommand : GerberCommand
    {
        private static readonly IDictionary<string, Func<IList<GerberExtendedCommandBlock>, GerberExtendedCommand>> Factories
            = new Dictionary<string, Func<IList<GerberExtendedCommandBlock>, GerberExtendedCommand>>();
        static GerberExtendedCommand()
        {
            Factories.Add(FormatStatementCommand.COMMAND_CODE, FormatStatementCommand.Create);
            Factories.Add(UnitCommand.COMMAND_CODE, UnitCommand.Create);
            Factories.Add(PolarityCommand.COMMAND_CODE, PolarityCommand.Create);
            Factories.Add(MirrorCommand.COMMAND_CODE, MirrorCommand.Create);
            Factories.Add(RotationCommand.COMMAND_CODE, RotationCommand.Create);
            Factories.Add(ScaleCommand.COMMAND_CODE, ScaleCommand.Create);
            Factories.Add(ApertureDefinitionCommand.COMMAND_CODE, ApertureDefinitionCommand.Create);
            Factories.Add(ApertureMacroDefinitionCommand.COMMAND_CODE, ApertureMacroDefinitionCommand.Create);
            Factories.Add(FileAttributeCommand.COMMAND_CODE, FileAttributeCommand.Create);
        }
        public IList<GerberExtendedCommandBlock> Blocks { get; protected set; }

        private string BlocksString
        {
            get { return string.Join("||", Blocks.Select(x => x.Text)); }
        }
        public static GerberExtendedCommand CreateCommand(ref int index, string data)
        {
            var curr = index;
            if (data[curr] != '%')
                return null;
            curr++;
            while (data[curr] != '%')
            {
                curr++;
            }
            var cmdText = data.Substring(index + 1, curr - index - 1);
            var code = cmdText.Substring(0, 2);
            var blocks = ExtractCommandBlocks(cmdText).ToList();
            var cmd = Factories.ContainsKey(code) ? Factories[code](blocks) : new GerberExtendedCommand();
            cmd.CommandCode = code;
            cmd.Text = cmdText;
            cmd.Blocks = new List<GerberExtendedCommandBlock>();
            foreach (var block in blocks)
            {
                cmd.Blocks.Add(block);
            }
            index = curr + 1;
            return cmd;
        }

        private static IEnumerable<GerberExtendedCommandBlock> ExtractCommandBlocks(string data)
        {
            var idx = 0;
            while (idx < data.Length)
            {
                var start = idx;
                while (data[idx] != '*')
                {
                    idx++;
                }
                yield return new GerberExtendedCommandBlock {Text = data.Substring(start, idx - start)};
                idx++;
                while (idx < data.Length && char.IsWhiteSpace(data, idx))
                {
                    idx++;
                }
            }
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return "%" + string.Join("*\r\n", Blocks.Select(x => x.Text)) + "*%";
        }
    }
}
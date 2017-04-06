using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GerberParser.Commands;

namespace GerberParser
{
    public class GerberFileObject
    {
        public IList<GerberCommand> Commands { get; private set; } = new List<GerberCommand>();

        public string FileName { get; private set; }

        public int IntPrecision { get; set; }
        public int DecPrecision { get; set; }

        public decimal Divisor { get; set; }
        public bool IsMetric { get; set; }

        public bool ReadFile(string filename)
        {
            FileName = filename;
            var data = File.ReadAllText(filename);
            int idx = 0;
            GerberCommand cmd;
            Commands = new List<GerberCommand>();
            Divisor = 1.0m;
            while (idx < data.Length && (cmd = GetNextCommand(ref idx, data)) != null)
            {
                Commands.Add(cmd);
                if (cmd is FormatStatementCommand)
                {
                    var fscmd = (FormatStatementCommand)cmd;
                    IntPrecision = fscmd.IntegerPositions;
                    DecPrecision = fscmd.DecimalPositions;
                    for (int i = 0; i < fscmd.DecimalPositions; i++)
                    {
                        Divisor *= 10.0m;
                    }
                }
                if (cmd is UnitCommand)
                {
                    var unitCmd = (UnitCommand)cmd;
                    IsMetric = unitCmd.IsMetric;
                }
                while (idx < data.Length && char.IsWhiteSpace(data, idx))
                {
                    idx++;
                }
            }
            var cnt =
                Commands.Where(
                    x =>
                        x.GetType() == typeof (GerberFunctionCodeCommand) ||
                        x.GetType() == typeof (GerberExtendedCommand)).ToList();
            if (cnt.Count > 0)
            {
                Console.WriteLine("WARNING! Unsupported commands were found!!!");
            }
            return true;
        }

        private GerberCommand GetNextCommand(ref int index, string data)
        {
            if (data[index] == '%')
            {
                //extended command
                return GerberExtendedCommand.CreateCommand(ref index, data);
            }
            return GerberFunctionCodeCommand.CreateCommand(ref index, data);
        }

    }

    [DebuggerDisplay("CommandBlock Text={Text}")]
    public class GerberExtendedCommandBlock
    {
        public string Text { get; set; }
    }
}

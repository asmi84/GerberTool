using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("FunctionCode Number={CommandNumber}, Text={Text}")]
    public class GerberFunctionCodeCommand : GerberCommand
    {
        private static readonly IDictionary<string, Func<string, GerberFunctionCodeCommand>> Factories
            = new Dictionary<string, Func<string, GerberFunctionCodeCommand>>();

        static GerberFunctionCodeCommand()
        {
            Factories.Add(CommendCommand.COMMAND_CODE, CommendCommand.Create);
            Factories.Add(LinearInterpolationCommand.COMMAND_CODE, LinearInterpolationCommand.Create);
            Factories.Add(OperationInterpolateCommand.COMMAND_CODE, OperationInterpolateCommand.Create);
            Factories.Add(OperationMoveCommand.COMMAND_CODE, OperationMoveCommand.Create);
            Factories.Add(OperationFlashCommand.COMMAND_CODE, OperationFlashCommand.Create);
            Factories.Add(RegionBeginCommand.COMMAND_CODE, RegionBeginCommand.Create);
            Factories.Add(RegionEndCommand.COMMAND_CODE, RegionEndCommand.Create);
            Factories.Add(FileEndCommand.COMMAND_CODE, FileEndCommand.Create);

            //obsolete commands
            Factories.Add(UnitInchCommand.COMMAND_CODE, UnitInchCommand.Create);
            Factories.Add(UnitMetricCommand.COMMAND_CODE, UnitMetricCommand.Create);
            Factories.Add(CoordinateAbsoluteCommand.COMMAND_CODE, CoordinateAbsoluteCommand.Create);
            Factories.Add(CoordinateIncrementalCommand.COMMAND_CODE, CoordinateIncrementalCommand.Create);

            Factories.Add(ClockwiseCircularInterpolationCommand.COMMAND_CODE, ClockwiseCircularInterpolationCommand.Create);
            Factories.Add(CounterclockwiseCircularInterpolationCommand.COMMAND_CODE, CounterclockwiseCircularInterpolationCommand.Create);
            Factories.Add(SingleQuadrantModeCommand.COMMAND_CODE, SingleQuadrantModeCommand.Create);
            Factories.Add(MultiQuadrantModeCommand.COMMAND_CODE, MultiQuadrantModeCommand.Create);
        }
        public static GerberFunctionCodeCommand CreateCommand(ref int index, string data)
        {
            var curr = index;
            while (data[curr] != '*')
            {
                curr++;
            }
            var cmdText = data.Substring(index, curr - index);
            var code = ExtractCommandCode(cmdText);
            GerberFunctionCodeCommand cmd = null;
            if (code.StartsWith("D"))
            {
                var num = int.Parse(code.Substring(1));
                if (num >= 10)
                {
                    cmd = new CurrentApertureCommand {Number = num};
                }
                switch (num)
                {
                    case 1:
                        cmd = OperationInterpolateCommand.Create(cmdText);
                        break;
                    case 2:
                        cmd = OperationMoveCommand.Create(cmdText);
                        break;
                    case 3:
                        cmd = OperationFlashCommand.Create(cmdText);
                        break;

                }
            }
            else if ((code.StartsWith("G01") || code.StartsWith("G02") || code.StartsWith("G03")) && data[index + 3] != '*')
            {
                cmd = Factories[code](code);
                cmd.CommandCode = code;
                cmd.Text = cmdText;
                index += 3;
                return cmd;
            }
            if (cmd == null)
                cmd = Factories.ContainsKey(code) ? Factories[code](cmdText) : new GerberFunctionCodeCommand();
            cmd.CommandCode = code;
            cmd.Text = cmdText;
            index = curr + 1;
            return cmd;
        }

        private static string ExtractCommandCode(string data)
        {
            var idx = 0;
            //TODO G03X2881738Y1568768I75238J-99796D01*
            while ((data[idx] != 'D') && (data[idx] != 'G') && data[idx] != 'M')
            {
                idx++;
            }
            var code = data[idx].ToString();
            idx++;
            var len = 0;
            while (idx + len < data.Length && char.IsDigit(data, idx + len))
            {
                len++;
            }
             code += data.Substring(idx, len);
            return code;
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            return Text + "*";
        }
    }
}
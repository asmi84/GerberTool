using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Operation Interpolate{DebuggerDisplay}")]
    public class OperationInterpolateCommand : GerberFunctionCodeCommand, IContainsUnits
    {
        public const string COMMAND_CODE = "D01";
        private static readonly Regex NumRegex = new Regex("([+-]{0,1}\\d{1,12})", RegexOptions.Compiled);

        private static decimal GetNumber(ref string str)
        {
            var numStr = NumRegex.Match(str).Groups[1].Value;
            str = str.Substring(numStr.Length);
            return decimal.Parse(numStr);
        }

        public static GerberFunctionCodeCommand Create(string data)
        {
            var cmdText = data.Replace(COMMAND_CODE, string.Empty);
            var cmd = new OperationInterpolateCommand();
            if (cmdText.StartsWith("X"))
            {
                cmdText = cmdText.Substring(1);
                cmd.X = GetNumber(ref cmdText);
                cmd.HasX = true;
            }
            if (cmdText.StartsWith("Y"))
            {
                cmdText = cmdText.Substring(1);
                cmd.Y = GetNumber(ref cmdText);
                cmd.HasY = true;
            }
            if (cmdText.StartsWith("I"))
            {
                cmdText = cmdText.Substring(1);
                cmd.OffsetX = GetNumber(ref cmdText);
            }
            if (cmdText.StartsWith("J"))
            {
                cmdText = cmdText.Substring(1);
                cmd.OffsetY = GetNumber(ref cmdText);
            }
            return cmd;
        }

        public bool HasX { get; protected set; }
        public decimal X { get; protected set; }
        public bool HasY { get; protected set; }
        public decimal Y { get; protected set; }
        public decimal OffsetX { get; protected set; }
        public decimal OffsetY { get; protected set; }

        private string DebuggerDisplay
        {
            get
            {
                var str = string.Empty;
                if (HasX)
                    str += $" X={X}";
                if (HasY)
                    str += $" Y={Y}";
                if (OffsetX != 0)
                    str += $" OffsetX={OffsetX}";
                if (OffsetY != 0)
                    str += $" OffsetY={OffsetY}";
                return str;
            }
        }
        public void MultiplyBy(decimal mul)
        {
            if (HasX)
                X *= mul;
            if (HasY)
                Y *= mul;
            OffsetX *= mul;
            OffsetY *= mul;
        }

        public void MoveBy(decimal offsetX, decimal offsetY)
        {
            if (HasX)
                X += offsetX;
            if (HasY)
                Y += offsetY;
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            var res = string.Empty;
            if (HasX)
                res += string.Format("X{0:F0}", X + offetX);
            if (HasY)
                res += string.Format("Y{0:F0}", Y + offsetY);
            if (OffsetX != 0)
                res += string.Format("I{0:F0}", OffsetX);
            if (OffsetY != 0)
                res += string.Format("J{0:F0}", OffsetY);
            res += "D01*";
            return res;
            //return base.ToStringWithOffset(offetX, offsetY);
        }
    }
}
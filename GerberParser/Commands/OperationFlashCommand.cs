using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GerberParser.Commands
{
    [DebuggerDisplay("Operation Flash, X={X}, Y={Y}")]
    public class OperationFlashCommand : GerberFunctionCodeCommand, IContainsUnits
    {
        public const string COMMAND_CODE = "D03";
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
            var cmd = new OperationFlashCommand();
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
            return cmd;
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            var res = "";
            if (HasX)
                res += string.Format("X{0:F0}", X + offetX);
            if (HasY)
                res += string.Format("Y{0:F0}", Y + offsetY);
            res += "D03*";
            return res;
            //return base.ToStringWithOffset(offetX, offsetY);
        }
        public void MultiplyBy(decimal mul)
        {
            if (HasX)
                X *= mul;
            if (HasY)
                Y *= mul;
        }

        public void MoveBy(decimal offsetX, decimal offsetY)
        {
            if (HasX)
                X += offsetX;
            if (HasY)
                Y += offsetY;
        }


        public bool HasX { get; protected set; }
        public decimal X { get; protected set; }
        public bool HasY { get; protected set; }
        public decimal Y { get; protected set; }
    }
}
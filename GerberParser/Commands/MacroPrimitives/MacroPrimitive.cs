using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GerberParser.Commands.MacroPrimitives
{
    public abstract class MacroPrimitive
    {
        public abstract string Id { get; }
        public bool IsSolid { get; protected set; }
        public decimal Rotation { get; protected set; }

        protected static string GetNextToken(ref string str)
        {
            int idx = 0;
            while (idx < str.Length && str[idx] != ',')
            {
                idx++;
            }
            var res = str.Substring(0, idx);
            str = idx < str.Length ? str.Substring(idx + 1).TrimStart() : string.Empty;
            return res;
        }

        public static decimal GetNextDecimalToken(ref string str)
        {
            var data = GetNextToken(ref str);
            return decimal.Parse(data);
        }

        public static int GetNextIntToken(ref string str)
        {
            var data = GetNextToken(ref str);
            return int.Parse(data);
        }

        protected abstract void Load(ref string str);
        public abstract void MultiplyBy(decimal mul);
        public abstract string GetStringInt();

        private static readonly HashSet<string> CodesWithoutPolarity
            = new HashSet<string>
            {
                CommentMacroPrimitive.CODE,
                MoireMacroPrimitive.CODE,
                ThermalMacroPrimitive.CODE,
            };

        public static MacroPrimitive Create(string str)
        {
            var code = GetNextToken(ref str);
            MacroPrimitive result = null;
            switch (code)
            {
                case CommentMacroPrimitive.CODE:
                    result = new CommentMacroPrimitive();
                    break;
                case OutlineMacroPrimitive.CODE:
                    result = new OutlineMacroPrimitive();
                    break;
                case MoireMacroPrimitive.CODE:
                    result = new MoireMacroPrimitive();
                    break;
                case ThermalMacroPrimitive.CODE:
                    result = new ThermalMacroPrimitive();
                    break;
            }
            if (result == null)
            {
                Console.WriteLine("WARNING! Unknown/unsupported macro primitive {0}", code);
                return null;
            }
            if (!CodesWithoutPolarity.Contains(code))
                result.IsSolid = GetNextToken(ref str) == "1";
            result.Load(ref str);
            if (code != CommentMacroPrimitive.CODE)
            {
                result.Rotation = GetNextDecimalToken(ref str);
            }

            return result;
        }

        public string GetString()
        {
            var str = Id;
            if (!CodesWithoutPolarity.Contains(Id))
            {
                str += "," + (IsSolid ? "1" : "0");
            }
            str += "," + GetStringInt();
            if (Id != CommentMacroPrimitive.CODE)
            {
                str += $",{Rotation}";
            }
            str += "*";
            return str;
        }
    }
}

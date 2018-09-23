using System.Diagnostics;

namespace GerberParser.Commands
{
    [DebuggerDisplay("GerberCommand Text={Text}")]
    public abstract class GerberCommand
    {
        public string Text { get; protected set; }
        public string CommandCode { get; protected set; }

        protected static decimal ExtractDecimal(ref string text)
        {
            var idx = 0;
            while (idx < text.Length && (char.IsDigit(text, idx) || text[idx] == '.'))
            {
                idx++;
            }
            var number = text.Substring(0, idx);
            text = text.Substring(idx);
            return decimal.Parse(number);
        }

        protected static int ExtractNumber(ref string text)
        {
            var idx = 0;
            while (idx < text.Length && char.IsDigit(text, idx))
            {
                idx++;
            }
            var number = text.Substring(0, idx);
            text = text.Substring(idx);
            return int.Parse(number);
        }

        public abstract string ToStringWithOffset(decimal offetX, decimal offsetY);

        public virtual bool IsObsolete()
        {
            if (GetType() == typeof (GerberExtendedCommand))
                return true;
            return false;
        }
    }
}
using System.Diagnostics;

namespace GerberParser.Commands.MacroPrimitives
{
    [DebuggerDisplay("CoordinatePair X = {X}, Y = {Y}")]
    public class CoordinatePair
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }

        public static CoordinatePair operator -(CoordinatePair a, CoordinatePair b)
        {
            return new CoordinatePair
            {
                X = a.X - b.X,
                Y = a.Y - b.Y
            };
        }
        public static CoordinatePair operator +(CoordinatePair a, CoordinatePair b)
        {
            return new CoordinatePair
            {
                X = a.X + b.X,
                Y = a.Y + b.Y
            };
        }

        public CoordinatePair Clone()
        {
            return new CoordinatePair { X = X, Y = Y };
        }
    }
}
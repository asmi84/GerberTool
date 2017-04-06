using System.Linq;

namespace GerberParser.Commands.MacroPrimitives
{
    public class OutlineMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "4";
        public override string Id => CODE;
        public CoordinatePair[] Points { get; private set; }

        public int PointsCount { get; private set; }


        protected override void Load(ref string str)
        {
            var ptCount = GetNextToken(ref str);
            PointsCount = int.Parse(ptCount);
            Points = new CoordinatePair[PointsCount + 1];
            for (int i = 0; i < PointsCount + 1; i++)
            {
                var pt = new CoordinatePair
                {
                    X = GetNextDecimalToken(ref str),
                    Y = GetNextDecimalToken(ref str),
                };
                Points[i] = pt;
            }
        }

        public override string GetStringInt()
        {
            return string.Join(",\r\n", new[] {PointsCount.ToString()}.Concat(Points.Select(x => $"{x.X},{x.Y}")));
        }

        public override void MultiplyBy(decimal mul)
        {
            foreach (var pair in Points)
            {
                pair.X *= mul;
                pair.Y *= mul;
            }
        }
    }
}
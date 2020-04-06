namespace GerberParser.Commands.MacroPrimitives
{
    public class CircleMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "1";
        public override string Id => CODE;
        public CoordinatePair Center { get; set; }
        public decimal Diameter { get; set; }
        protected override void Load(ref string str)
        {
            Diameter = GetNextDecimalToken(ref str);
            Center = new CoordinatePair
            {
                X = GetNextDecimalToken(ref str),
                Y = GetNextDecimalToken(ref str),
            };
        }

        public override void MultiplyBy(decimal mul)
        {
            Diameter *= mul;
            Center.X *= mul;
            Center.Y *= mul;
        }

        public override string GetStringInt()
        {
            return $"{Diameter},{Center.X},{Center.Y}";
        }
    }
}
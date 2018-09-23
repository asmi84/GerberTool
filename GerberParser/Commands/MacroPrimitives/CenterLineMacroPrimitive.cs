namespace GerberParser.Commands.MacroPrimitives
{
    public class CenterLineMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "21";
        public override string Id => CODE;

        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public CoordinatePair Center { get; set; }
        protected override void Load(ref string str)
        {
            Width = GetNextDecimalToken(ref str);
            Height = GetNextDecimalToken(ref str);
            Center = new CoordinatePair
            {
                X = GetNextDecimalToken(ref str),
                Y = GetNextDecimalToken(ref str),
            };
        }

        public override void MultiplyBy(decimal mul)
        {
            Width *= mul;
            Height *= mul;
            Center.X *= mul;
            Center.Y *= mul;
        }

        public override string GetStringInt()
        {
            return $"{Width},{Height},{Center.X},{Center.Y}";
        }
    }
}
namespace GerberParser.Commands.MacroPrimitives
{
    public class ThermalMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "7";
        public override string Id => CODE;
        public CoordinatePair Center { get; private set; }
        public decimal OuterDiameter { get; set; }
        public decimal InnerDiameter { get; set; }
        public decimal GapThickness { get; set; }
        protected override void Load(ref string str)
        {
            Center = new CoordinatePair
            {
                X = GetNextDecimalToken(ref str),
                Y = GetNextDecimalToken(ref str)
            };
            OuterDiameter = GetNextDecimalToken(ref str);
            InnerDiameter = GetNextDecimalToken(ref str);
            GapThickness = GetNextDecimalToken(ref str);
        }

        public override void MultiplyBy(decimal mul)
        {
            Center.X *= mul;
            Center.Y *= mul;
            OuterDiameter *= mul;
            InnerDiameter *= mul;
            GapThickness *= mul;
        }

        public override string GetStringInt()
        {
            var str = $"{Center.X},{Center.Y},{OuterDiameter},{InnerDiameter},{GapThickness}";
            return str;
        }
    }
}
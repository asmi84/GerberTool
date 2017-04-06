namespace GerberParser.Commands.MacroPrimitives
{
    public class MoireMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "6";
        public override string Id => CODE;

        public CoordinatePair Center { get; private set; }
        public decimal OuterRingDiameter { get; set; }
        public decimal RingThickness { get; set; }
        public decimal GapBetweenRings { get; set; }
        public int MaxNumberOfRings { get; set; }
        public decimal CrosshairThickness { get; set; }
        public decimal CrosshairLength { get; set; }

        protected override void Load(ref string str)
        {
            Center = new CoordinatePair
            {
                X = GetNextDecimalToken(ref str),
                Y = GetNextDecimalToken(ref str)
            };
            OuterRingDiameter = GetNextDecimalToken(ref str);
            RingThickness = GetNextDecimalToken(ref str);
            GapBetweenRings = GetNextDecimalToken(ref str);
            MaxNumberOfRings = GetNextIntToken(ref str);
            CrosshairThickness = GetNextDecimalToken(ref str);
            CrosshairLength = GetNextDecimalToken(ref str);
        }

        public override void MultiplyBy(decimal mul)
        {
            Center.X *= mul;
            Center.Y *= mul;
            OuterRingDiameter *= mul;
            RingThickness *= mul;
            GapBetweenRings *= mul;
            CrosshairThickness *= mul;
            CrosshairLength *= mul;
        }

        public override string GetStringInt()
        {
            var str = $"{Center.X},{Center.Y},{OuterRingDiameter},{RingThickness},{GapBetweenRings},{MaxNumberOfRings},";
            str += $"{CrosshairThickness},{CrosshairLength}";
            return str;
        }
    }
}
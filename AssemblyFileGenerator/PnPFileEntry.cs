namespace AssemblyFileGenerator
{
    public class PnPFileEntry
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }

        public int Rotation { get; set; }
        public bool IsTopSide { get; set; }
        public string FootprintName { get; set; }
        public string RefDes { get; set; }
        public string Value { get; set; }
    }
}
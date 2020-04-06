using System.Collections.Generic;

namespace AssemblyFileGenerator
{
    public class PnPFootprintGroup
    {
        private readonly string _footprintName;
        private readonly string _value;
        private readonly bool _isTopSide;
        private readonly int _count;
        private readonly List<string> _refDes;
        private readonly List<PnPFileEntry> _parts;

        public string FootprintName
        {
            get { return _footprintName; }
        }

        public string Value
        {
            get { return _value; }
        }

        public bool IsTopSide
        {
            get { return _isTopSide; }
        }

        public int Count
        {
            get { return _count; }
        }

        public List<string> RefDes
        {
            get { return _refDes; }
        }

        public List<PnPFileEntry> Parts
        {
            get { return _parts; }
        }

        public PnPFootprintGroup(string footprintName, string value, bool isTopSide, int count, List<string> refDes, List<PnPFileEntry> parts)
        {
            _footprintName = footprintName;
            _value = value;
            _isTopSide = isTopSide;
            _count = count;
            _refDes = refDes;
            _parts = parts;
        }
    }
}

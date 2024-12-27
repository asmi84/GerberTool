using System;

namespace AssemblyFileGenerator
{
    public class PnPDataFilter
    {
        private readonly string _refTemplate;
        private readonly bool _isWildcard;

        public PnPDataFilter(string refTemplate)
        {
            _isWildcard = false;
            _refTemplate = refTemplate;
            if (_refTemplate.EndsWith("*"))
            {
                _isWildcard = true;
                _refTemplate = _refTemplate.Replace("*", string.Empty);
            }
        }

        public bool Match(string refName)
        {
            if (_isWildcard)
                return refName.StartsWith(_refTemplate, StringComparison.InvariantCultureIgnoreCase);
            return refName.Equals(_refTemplate, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssemblyFileGenerator
{
    public class FootprintComparer : IComparer<string>
    {
        private readonly ProjectType projectType;

        private static readonly Regex orcad_smd_passive_regex = new Regex("smd_(\\w+)_(\\d{4})", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex orcad_smd_led_regex = new Regex("led_(\\d{4})", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex altium_smd_passive_regex = new Regex("(\\w{3})C(\\d{4})", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex kicad_smd_passive_regex = new Regex("(\\w{1})_(\\d{4})_(\\d{4})", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public FootprintComparer(ProjectType projType)
        {
            projectType = projType;
        }

        public int Compare(string x, string y)
        {
            switch (projectType)
            {
                case ProjectType.Orcad: return CompareOrcad(x, y);
                case ProjectType.KiCad: return CompareKicad(x, y);
                case ProjectType.Altium: return CompareAltium(x, y);
                default: return CompareKicad(x, y);
            }
            //return _isOrcad ? CompareOrcad(x, y) : CompareKicad(x, y);
        }

        private bool KiCadIsPassive(string x)
        {
            return x.StartsWith("R_", StringComparison.CurrentCultureIgnoreCase)
                || x.StartsWith("C_", StringComparison.CurrentCultureIgnoreCase)
                || x.StartsWith("L_", StringComparison.CurrentCultureIgnoreCase);
        }

        public int CompareKicad(string x, string y)
        {
            var x_smd_passive = KiCadIsPassive(x);
            var y_smd_passive = KiCadIsPassive(y);
            if (x_smd_passive & !y_smd_passive)
                return -1;
            if (!x_smd_passive & y_smd_passive)
                return 1;
            var xval = KiCadTryExtractSMDSize(x, out var xtype);
            var yval = KiCadTryExtractSMDSize(y, out var ytype);
            if (xval.HasValue && yval.HasValue)
                return xval.Value.CompareTo(yval.Value);
            //if (!x_smd_passive && !y_smd_passive)
            return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
        }

        private int? KiCadTryExtractSMDSize(string str, out string partType)
        {
            partType = null;
            var match = kicad_smd_passive_regex.Match(str);
            if (match.Success)
            {
                partType = match.Groups[1].Value;
                return int.Parse(match.Groups[2].Value);
            }
            return null;
        }

        private int? TryExtractSMDSize(string str)
        {
            var match = orcad_smd_passive_regex.Match(str);
            if (match.Success)
                return int.Parse(match.Groups[2].Value);
            match = orcad_smd_led_regex.Match(str);
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return null;
        }

        public int CompareOrcad(string x, string y)
        {
            var x_smd_passive = x.StartsWith("smd_res", StringComparison.CurrentCultureIgnoreCase)
                | x.StartsWith("smd_cap", StringComparison.CurrentCultureIgnoreCase)
                | x.StartsWith("led", StringComparison.CurrentCultureIgnoreCase)
                | x.StartsWith("smd_bead", StringComparison.CurrentCultureIgnoreCase);
            var y_smd_passive = y.StartsWith("smd_res", StringComparison.CurrentCultureIgnoreCase)
                | y.StartsWith("smd_cap", StringComparison.CurrentCultureIgnoreCase)
                | y.StartsWith("led", StringComparison.CurrentCultureIgnoreCase)
                | y.StartsWith("smd_bead", StringComparison.CurrentCultureIgnoreCase);
            if (x_smd_passive & !y_smd_passive)
                return -1;
            if (!x_smd_passive & y_smd_passive)
                return 1;
            var xval = TryExtractSMDSize(x);
            var yval = TryExtractSMDSize(y);
            if (xval.HasValue && yval.HasValue)
                return xval.Value.CompareTo(yval.Value);
            //if (!x_smd_passive && !y_smd_passive)
            return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
            /*var x_m = smd_passive_regex.Match(x);
            var y_m = smd_passive_regex.Match(y);
            if (!x_m.Success || !y_m.Success)
                return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);*/

        }

        private static bool AltiumIsPassive(string x)
        {
            return x.StartsWith("CAPC", StringComparison.CurrentCultureIgnoreCase)
                || x.StartsWith("RESC", StringComparison.CurrentCultureIgnoreCase)
                || x.StartsWith("INDC", StringComparison.CurrentCultureIgnoreCase);
        }

        private int? AltiumTryExtractSMDSize(string str, out string partType)
        {
            partType = null;
            var match = altium_smd_passive_regex.Match(str);
            if (match.Success)
            {
                partType = match.Groups[1].Value;
                return int.Parse(match.Groups[2].Value);
            }
            return null;
        }

        private int CompareAltium(string x, string y)
        {
            var x_smd_passive = AltiumIsPassive(x);
            var y_smd_passive = AltiumIsPassive(y);
            if (x_smd_passive & !y_smd_passive)
                return -1;
            if (!x_smd_passive & y_smd_passive)
                return 1;
            var xval = AltiumTryExtractSMDSize(x, out var xtype);
            var yval = AltiumTryExtractSMDSize(y, out var ytype);
            if (xval.HasValue && yval.HasValue)
                return xval.Value.CompareTo(yval.Value);
            //if (!x_smd_passive && !y_smd_passive)
            return StringComparer.CurrentCultureIgnoreCase.Compare(x, y);
        }
    }
}

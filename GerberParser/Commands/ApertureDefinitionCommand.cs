using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GerberParser.Commands
{
    [DebuggerDisplay("ApertureDefinition Code=D{Number} {DebuggerDisplay}")]
    public class ApertureDefinitionCommand : GerberExtendedCommand, IContainsUnits
    {
        public const string COMMAND_CODE = "AD";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            text = text.Substring(1); //"D" 
            var idx = 0;
            while (idx < text.Length && char.IsDigit(text, idx))
            {
                idx++;
            }
            var number = text.Substring(0, idx);
            text = text.Substring(idx);
            string apTemplate;
            if (text[1] == ',')
                apTemplate = text.Substring(0, 1);
            else
                apTemplate = "M";
            var cmd = new ApertureDefinitionCommand {Number = int.Parse(number), Template = apTemplate};
            switch (apTemplate)
            {
                case "C":
                    text = text.Substring(2);
                    cmd.Diameter = ExtractDecimal(ref text);
                    break;
                case "R":
                case "O":
                    text = text.Substring(2);
                    cmd.SizeX = ExtractDecimal(ref text);
                    text = text.Substring(1); // "X" symbol
                    cmd.SizeY = ExtractDecimal(ref text);
                    break;
                case "P":
                    text = text.Substring(2);
                    cmd.Diameter = ExtractDecimal(ref text);
                    text = text.Substring(1); // "X" symbol
                    cmd.VerticesCount = ExtractNumber(ref text);
                    if (text.Length > 0)
                    {
                        text = text.Substring(1); // "X" symbol
                        cmd.RotationAngle = ExtractDecimal(ref text);
                    }
                    break;
                default:
                    //this is macro
                    cmd.Template = "M";
                    cmd.MacroName = text;
                    text = string.Empty;
                    break;
            }
            if (text.Length > 0)
            {
                text = text.Substring(1); // "X" symbol
                cmd.HoleDiameter = ExtractDecimal(ref text);
            }
            return cmd;
        }

        public void GetApertureExtents(out decimal sizeX, out decimal sizeY)
        {
            sizeX = sizeY = 0;
            switch (Template)
            {
                case "C":
                case "P":
                    sizeX = sizeY = Diameter;
                    break;
                case "R":
                case "O":
                    sizeX = SizeX;
                    sizeY = SizeY;
                    break;
            }
        }

        public int Number { get; set; }
        public string Template { get; protected set; }
        public string MacroName { get; set; }

        public decimal Diameter { get; protected set; }
        public decimal SizeX { get; protected set; }
        public decimal SizeY { get; protected set; }

        public int VerticesCount { get; protected set; }
        public decimal RotationAngle { get; protected set; }
        public decimal HoleDiameter { get; protected set; }

        private string DebuggerDisplay
        {
            get
            {
                var str = string.Empty;
                switch (Template)
                {
                    case "C":
                        str += $"Circle Diameter={Diameter}";
                        break;
                    case "R":
                        str += $"Rectangle SizeX={SizeX}, SizeY={SizeY}";
                        break;
                    case "O":
                        str += $"Obround SizeX={SizeX}, SizeY={SizeY}";
                        break;
                    case "P":
                        str += $"Polygon Diameter={Diameter}, Vertices={VerticesCount}";
                        if (RotationAngle != 0)
                        {
                            str += $" Rotation={RotationAngle}";
                        }
                        break;
                    case "M":
                        str += $"Macro Name={MacroName}";
                        break;
                }
                if (HoleDiameter != 0)
                    str += $" Hole={HoleDiameter}";
                return str;
            }
        }

        public string GetString()
        {
            return DebuggerDisplay;
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            var str = $"%{COMMAND_CODE}D{Number}";
            switch (Template)
            {
                case "C":
                    str += $"{Template},{Diameter}";
                    break;
                case "R":
                case "O":
                    str += $"{Template},{SizeX}X{SizeY}";
                    break;
                case "P":
                    str += $"{Template},{Diameter}X{VerticesCount}";
                    if (RotationAngle != 0)
                        str += $"X{RotationAngle}";
                    break;
                case "M":
                    str += $"{MacroName}";
                    break;
            }
            if (HoleDiameter != 0)
                str += $"X{HoleDiameter}";
            str += "*%";
            return str;
        }

        public void MultiplyBy(decimal mul)
        {
            switch (Template)
            {
                case "C":
                case "P":
                    Diameter *= mul;
                    break;
                case "R":
                case "O":
                    SizeX *= mul;
                    SizeY *= mul;
                    break;
            }
            HoleDiameter *= mul;
        }

        public void MoveBy(decimal offsetX, decimal offsetY)
        {
            //do nothing
        }
    }
}
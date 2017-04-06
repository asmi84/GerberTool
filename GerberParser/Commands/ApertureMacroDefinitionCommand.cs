using System.Collections.Generic;
using System.Linq;
using GerberParser.Commands.MacroPrimitives;

namespace GerberParser.Commands
{
    public class ApertureMacroDefinitionCommand : GerberExtendedCommand, IContainsUnits
    {
        public const string COMMAND_CODE = "AM";

        public static GerberExtendedCommand Create(IList<GerberExtendedCommandBlock> blocks)
        {
            var text = blocks.First().Text.Substring(2);
            
            var cmd = new ApertureMacroDefinitionCommand();
            cmd.Name = text;
            cmd.Primitives = new List<MacroPrimitive>();
            foreach (var block in blocks.Skip(1))
            {
                var primitive = MacroPrimitive.Create(block.Text);
                if (primitive != null)
                    cmd.Primitives.Add(primitive);
            }
            return cmd;
        }
        public IList<MacroPrimitive> Primitives { get; private set; }

        public string Name { get; set; }

        public void MultiplyBy(decimal mul)
        {
            foreach (var primitive in Primitives)
            {
                primitive.MultiplyBy(mul);
            }
        }

        public override string ToStringWithOffset(decimal offetX, decimal offsetY)
        {
            var str = $"%{COMMAND_CODE}{Name}*\r\n";
            str += string.Join("\r\n", Primitives.Select(x => x.GetString()));
            str += "%";
            return str;
        }

        public void MoveBy(decimal offsetX, decimal offsetY)
        {
            //Do nothing
        }
    }
}
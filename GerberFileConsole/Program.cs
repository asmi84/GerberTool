using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GerberParser;
using GerberParser.Commands;

namespace GerberFileConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var fname = "iCE40 Ultra Audio.v2-In2.Cu.g3";
            fname = "iCE40 Ultra Audio.v2-F.Paste.gtp";
            var second_fname = "TopPaste.nRF24L01P.gbr";
            //second_fname = "Top.gbr";
            //fname = "Bottom.gbr";
            fname = "TopSilk.gbr";
            //fname = "Polarities_and_Apertures.gbr";
            if (args.Length > 0)
                fname = args[0];
            if (args.Length > 1)
                second_fname = args[1];
            var obj = new GerberFileObject();
            //obj.ReadFile("Bottom.gbr");
            //obj.ReadFile("TopSilk.gbr");
            obj.ReadFile(fname);
            GerberFileRender.CreateImage(obj, 300);
            return;
            decimal minX, maxX, minY, maxY;
            GerberFileProcessor.CalculateExtents(obj, out minX, out maxX, out minY, out maxY);
            var sizeX = maxX - minX;
            var sizeY = maxY - minY;
            var divisor = obj.Divisor;
            var mainOffsetX = -minX + 10.0m;
            var mainOffsetY = -minY + 10.0m;
            GerberFileProcessor.Transpose(obj, mainOffsetX, mainOffsetY);

            var secondObj = new GerberFileObject();
            secondObj.ReadFile(second_fname);
            var resObj = GerberFileProcessor.MergeFiles(obj, secondObj, sizeX + (obj.IsMetric ? 10.0m : 0.4m), 0.0m);
            using (var sw = new StreamWriter("test.gbr", false))
            {
                foreach (var cmd in resObj.Commands.Where(x => !x.IsObsolete()))
                {
                    sw.WriteLine(cmd.ToStringWithOffset(0.0m, 0.0m));
                }
            }
            return;
            using (var sw = new StreamWriter("test.gbr", false))
            {
                GerberFileProcessor.ConvertUnits(obj, true);
                GerberFileProcessor.CalculateExtents(obj, out minX, out maxX, out minY, out maxY);
                Console.WriteLine("MinX={0}{4}, MaxX={1}{4}, MinY = {2}{4}, MaxY = {3}{4}", minX, maxX, minY, maxY, obj.IsMetric ? "mm" : "in");
                Console.WriteLine("Width={0}{2}, Height={1}{2}", maxX - minX, maxY - minY, obj.IsMetric ? "mm" : "in");
                foreach (var cmd in obj.Commands.Where(x => !x.IsObsolete()))
                {
                    sw.WriteLine(cmd.ToStringWithOffset(0.0m, 0.0m));
                }
            }
            return;

            var firstIndex = 0;
            var spacing = obj.IsMetric ? 10.0m : 0.4m;
            for (int index = 0; index < obj.Commands.Count - 1; index++)
            {
                var cmd = obj.Commands[index];
                if (cmd is CurrentApertureCommand || cmd is OperationMoveCommand || cmd is OperationFlashCommand || cmd is OperationInterpolateCommand)
                {
                    firstIndex = index;
                    break;
                }
                //sw.WriteLine(cmd.ToStringWithOffset(0, 0));
            }
            using (var sw = new StreamWriter("test.gbr", false))
            {

                var baseOffsetX = -minX * divisor;
                var baseOffsetY = -minY * divisor;

                var offsetX = (sizeX + spacing) * divisor + baseOffsetX;
                var offsetY = (sizeY + spacing) * divisor + baseOffsetY;

                for (int index = 0; index < obj.Commands.Count - 1/*skip last command M02 (EOF mark)*/; index++)
                {
                    var cmd = obj.Commands[index];
                    sw.WriteLine(cmd.ToStringWithOffset(baseOffsetX, baseOffsetY));
                }

                for (int index = firstIndex; index < obj.Commands.Count; index++)
                {
                    var cmd = obj.Commands[index];
                    sw.WriteLine(cmd.ToStringWithOffset(baseOffsetX, offsetY));
                }

                for (int index = firstIndex; index < obj.Commands.Count; index++)
                {
                    var cmd = obj.Commands[index];
                    sw.WriteLine(cmd.ToStringWithOffset(offsetX, baseOffsetY));
                }

                for (int index = firstIndex; index < obj.Commands.Count; index++)
                {
                    var cmd = obj.Commands[index];
                    sw.WriteLine(cmd.ToStringWithOffset(offsetX, offsetY));
                }
            }

        }
    }
}

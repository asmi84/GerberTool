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
        private const string EXE_NAME = "GerberFileConsole.exe";
        private const string MODE_RENDER = "-r";

        private static readonly IDictionary<string, Action<string[]>> Modes
            = new Dictionary<string, Action<string[]>>
            {
                {MODE_RENDER, ModeRender },
            };

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0].StartsWith("-h"))
            {
                DisplayUsage();
                return;
            }
            var mode = args[0];
            if (!Modes.ContainsKey(mode))
            {
                Console.WriteLine($"ERROR - invalid mode '{mode}'");
                return;
            }
            Modes[mode](args);
            return;
            /*decimal minX, maxX, minY, maxY;
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

                //skip last command M02 (EOF mark)
                for (int index = 0; index < obj.Commands.Count - 1; index++)
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
            }*/

        }

        private static void DisplayUsage()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine($"{EXE_NAME} [mode] ...");
            Console.WriteLine("Available modes:");
            Console.WriteLine($"\t{MODE_RENDER} - render file");
            Console.WriteLine($"\t\t{EXE_NAME} {MODE_RENDER} file_name [out_file_name=file_name.png] [scale=100]");
        }

        private static void ModeRender(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR - file name is missing.");
                return;
            }
            var fname = args[1];
            if (!File.Exists(fname))
            {
                Console.WriteLine($"ERROR - file '{fname}' is not found.");
                return;
            }
            var resultName = fname + ".png";
            if (args.Length > 2)
                resultName = args[2];
            var scale = 100.0m;
            if (args.Length > 3 && !decimal.TryParse(args[3], out scale))
            {
                scale = 100.0m;
            }
            Console.WriteLine($"Rendering file '{fname}' with scale of '{scale}', saving into '{resultName}'");
            var obj = new GerberFileObject();
            obj.ReadFile(fname);
            try
            {
                GerberFileRender.CreateImage(obj, scale, resultName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}

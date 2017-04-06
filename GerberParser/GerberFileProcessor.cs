using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GerberParser.Commands;

namespace GerberParser
{
    public static class GerberFileProcessor
    {
        public static void CalculateExtents(GerberFileObject fileObject, 
            out decimal minX, out decimal maxX, out decimal minY, out decimal maxY)
        {
            var currX = 0.0m;
            var currY = 0.0m;
            var divisor = fileObject.Divisor;
            minX = decimal.MaxValue;
            maxX = decimal.MinValue;
            minY = decimal.MaxValue;
            maxY = decimal.MinValue;
            var coordsChanged = false;

            var apertures = new Dictionary<int, ApertureDefinitionCommand>();

            ApertureDefinitionCommand currAperture = null;

            foreach (var command in fileObject.Commands)
            {
                if (command is ApertureDefinitionCommand)
                {
                    var adCmd = (ApertureDefinitionCommand)command;
                    apertures.Add(adCmd.Number, adCmd);
                }
                if (command is CurrentApertureCommand)
                {
                    var caCmd = (CurrentApertureCommand)command;
                    currAperture = apertures[caCmd.Number];
                }
                if (command is OperationInterpolateCommand)
                {
                    var drawCmd = (OperationInterpolateCommand) command;
                    if (drawCmd.HasX)
                        currX = drawCmd.X / divisor;
                    if (drawCmd.HasY)
                        currY = drawCmd.Y / divisor;
                    coordsChanged = true;
                }
                if (command is OperationMoveCommand)
                {
                    var drawCmd = (OperationMoveCommand) command;
                    if (drawCmd.HasX)
                        currX = drawCmd.X / divisor;
                    if (drawCmd.HasY)
                        currY = drawCmd.Y / divisor;
                    coordsChanged = true;
                }
                if (command is OperationFlashCommand)
                {
                    var drawCmd = (OperationFlashCommand) command;
                    if (drawCmd.HasX)
                        currX = drawCmd.X / divisor;
                    if (drawCmd.HasY)
                        currY = drawCmd.Y / divisor;
                    coordsChanged = true;
                }
                if (coordsChanged)
                {
                    var currApSizeX = 0.0m;
                    var currApSizeY = 0.0m;
                    if (currAperture != null)
                    {
                        currAperture.GetApertureExtents(out currApSizeX, out currApSizeY);
                        currApSizeX /= 2;
                        currApSizeY /= 2;
                    }
                    if (currX - currApSizeX < minX)
                        minX = currX - currApSizeX;
                    if (currX + currApSizeX > maxX)
                        maxX = currX + currApSizeX;
                    if (currY - currApSizeY < minY)
                        minY = currY - currApSizeY;
                    if (currY + currApSizeY > maxY)
                        maxY = currY + currApSizeY;
                    coordsChanged = false;
                }
            }
        }

        public static void ConvertUnits(GerberFileObject fileObject, bool toMetric)
        {
            var divisor = 1.0m;
            var scaleFactor = 1.0m;
            foreach (var command in fileObject.Commands)
            {
                if (command is FormatStatementCommand)
                {
                    var fscmd = (FormatStatementCommand)command;
                    for (int i = 0; i < fscmd.DecimalPositions; i++)
                    {
                        divisor *= 10.0m;
                    }
                    scaleFactor = toMetric ? 25.4m : Math.Round(1.0m / 25.4m);
                    fscmd.IntegerPositions = toMetric ? 4 : 2;
                    fscmd.DecimalPositions = 6;
                }
                if (command is UnitCommand)
                {
                    var unitCmd = (UnitCommand)command;
                    //no need to convert if units are already same as requested
                    if ((toMetric && unitCmd.IsMetric) || (!toMetric && !unitCmd.IsMetric))
                        return;
                    scaleFactor = toMetric ? 25.4m : Math.Round(1.0m / 25.4m);
                    unitCmd.IsMetric = toMetric;
                }
                if (command is IContainsUnits)
                {
                    var intf = (IContainsUnits)command;
                    intf.MultiplyBy(scaleFactor);
                }
            }
            fileObject.IsMetric = toMetric;
        }

        public static void Transpose(GerberFileObject obj, decimal offsetX, decimal offsetY)
        {

            offsetX *= obj.Divisor;
            offsetY *= obj.Divisor;
            foreach (var cmd in obj.Commands)
            {
                if (cmd is IContainsUnits)
                {
                    var unitCmds = (IContainsUnits)cmd;
                    unitCmds.MoveBy(offsetX, offsetY);
                }
            }
        }

        public static GerberFileObject MergeFiles(GerberFileObject mainObj, GerberFileObject secondObj, decimal offsetX, decimal offsetY)
        {
            if (!mainObj.IsMetric)
                ConvertUnits(mainObj, true);
            else if (!secondObj.IsMetric)
                ConvertUnits(secondObj, true);

            decimal minX, maxX, minY, maxY;
            CalculateExtents(secondObj, out minX, out maxX, out minY, out maxY);
            offsetX -= minX;
            offsetY -= minY;

            offsetX *= mainObj.Divisor;
            offsetY *= mainObj.Divisor;

            var apMap = new Dictionary<string, int>();
            var apertures = new Dictionary<int, ApertureDefinitionCommand>();
            var maxNum = 10;

            var apMacros = new Dictionary<string, ApertureMacroDefinitionCommand>();

            foreach (var cmd in mainObj.Commands.OfType<ApertureMacroDefinitionCommand>())
            {
                apMacros.Add(cmd.Name, cmd);
            }
            var secondApMacroMap = new Dictionary<string, string>(); //local map -> global map
            foreach (var cmd in secondObj.Commands.OfType<ApertureMacroDefinitionCommand>())
            {
                var name = cmd.Name;
                if (!apMacros.ContainsKey(name))
                {
                    apMacros.Add(cmd.Name, cmd);
                    secondApMacroMap.Add(name, name);
                }
                else
                {
                    var newName = name + "_";
                    apMacros.Add(newName, cmd);
                    secondApMacroMap.Add(name, newName);
                    cmd.Name = newName;
                }
            }

            foreach (var cmd in mainObj.Commands.OfType<ApertureDefinitionCommand>())
            {
                apertures.Add(cmd.Number, cmd);
                apMap.Add(cmd.GetString(), cmd.Number);
                if (cmd.Number > maxNum)
                    maxNum = cmd.Number;
            }

            var secondMap = new Dictionary<int, int>(); //local map -> global map
            foreach (var cmd in secondObj.Commands.OfType<ApertureDefinitionCommand>())
            {
                var key = cmd.GetString();
                if (!apMap.ContainsKey(key))
                {
                    var newNum = maxNum + 1;
                    apertures.Add(newNum, cmd);
                    apMap.Add(key, newNum);
                    secondMap.Add(cmd.Number, newNum);
                    cmd.Number = newNum;
                    if (cmd.Number > maxNum)
                        maxNum = cmd.Number;
                }
                else
                {
                    var globalNum = apMap[key];
                    secondMap.Add(cmd.Number, globalNum);
                    cmd.Number = globalNum;
                }
                if (cmd.Template == "M" && !string.IsNullOrEmpty(cmd.MacroName))
                {
                    var newName = secondApMacroMap[cmd.MacroName];
                    cmd.MacroName = newName;
                }
            }

            var resultObj = new GerberFileObject();
            resultObj.IsMetric = true;
            resultObj.IntPrecision = 4;
            resultObj.DecPrecision = 6;
            resultObj.Divisor = mainObj.Divisor;
            resultObj.Commands.Add(UnitCommand.Init(true));
            resultObj.Commands.Add(FormatStatementCommand.Init(resultObj.IntPrecision, resultObj.DecPrecision));

            foreach (var amCmd in apMacros.Values)
            {
                resultObj.Commands.Add(amCmd);
            }

            foreach (var apCmd in apertures.Values)
            {
                resultObj.Commands.Add(apCmd);
            }

            //these commands are already added above
            var excludeCmd = new HashSet<string>
            {
                "G04", //comment
                "MO",  //unit
                "AM",  //aperture macro
                "AD",  //aperture define
                "FS"   //format specifier
            };

            foreach (var cmd in mainObj.Commands.Where(x => !excludeCmd.Contains(x.CommandCode) && !x.IsObsolete()))
            {
                if (cmd is FileEndCommand)
                    continue;
                resultObj.Commands.Add(cmd);
            }

            foreach (var cmd in secondObj.Commands.Where(x => !excludeCmd.Contains(x.CommandCode) && !x.IsObsolete()))
            {
                if (cmd is CurrentApertureCommand)
                {
                    var caCmd = (CurrentApertureCommand) cmd;
                    caCmd.Number = secondMap[caCmd.Number];
                }
                if (cmd is IContainsUnits)
                {
                    var unitCmds = (IContainsUnits) cmd;
                    unitCmds.MoveBy(offsetX, offsetY);
                }
                resultObj.Commands.Add(cmd);
            }

            return resultObj;
        }
    }
}
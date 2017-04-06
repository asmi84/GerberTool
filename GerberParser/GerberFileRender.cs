using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using GerberParser.Commands;
using GerberParser.Commands.MacroPrimitives;

namespace GerberParser
{
    public static class GerberFileRender
    {


        private static Brush GetBrushColor(bool isSolid)
        {
            return isSolid ? Brushes.Green : Brushes.Black;
        }

        public static void CreateImage(GerberFileObject fileObject, int scale)
        {
            decimal minX, maxX, minY, maxY;
            GerberFileProcessor.CalculateExtents(fileObject, out minX, out maxX, out minY, out maxY);

            var border = fileObject.IsMetric ? 10.0m : 0.5m;

            var offsetX = -minX;
            var offsetY = -minY;

            var state = new GraphicsState(scale, border, offsetX, offsetY);
            state.Divisor = fileObject.Divisor;

            var sizeX = (int)Math.Round((maxX - minX + border * 2) * scale);
            var sizeY = (int)Math.Round((maxY - minY + border * 2) * scale);
            var img = new Bitmap(sizeX, sizeY);
            var gx = Graphics.FromImage(img);
            gx.Clear(Color.Black);
            state.GraphObject = gx;

            foreach (var cmd in fileObject.Commands)
            {
                ProcessCommand(cmd as FormatStatementCommand, state);
                ProcessCommand(cmd as ApertureDefinitionCommand, state);
                ProcessCommand(cmd as ApertureMacroDefinitionCommand, state);
                ProcessCommand(cmd as PolarityCommand, state);
                ProcessCommand(cmd as LinearInterpolationCommand, state);
                ProcessCommand(cmd as ClockwiseCircularInterpolationCommand, state);
                ProcessCommand(cmd as CounterclockwiseCircularInterpolationCommand, state);
                ProcessCommand(cmd as SingleQuadrantModeCommand, state);
                ProcessCommand(cmd as MultiQuadrantModeCommand, state);
                ProcessCommand(cmd as CurrentApertureCommand, state);
                ProcessCommand(cmd as RegionBeginCommand, state);
                ProcessCommand(cmd as RegionEndCommand, state);
                ProcessCommand(cmd as OperationInterpolateCommand, state);
                ProcessCommand(cmd as OperationMoveCommand, state);
                ProcessCommand(cmd as OperationFlashCommand, state);
            }
            img.Save("test.bmp");
        }

        private static void ProcessCommand(FormatStatementCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IntPrecision = cmd.IntegerPositions;
            state.DecPrecision = cmd.DecimalPositions;
        }

        private static void ProcessCommand(ApertureDefinitionCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Apertures.Add(cmd.Number, cmd);
        }

        private static void ProcessCommand(ApertureMacroDefinitionCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.ApertureMacros.Add(cmd.Name, cmd);
        }

        private static void ProcessCommand(PolarityCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsDarkPolarity = cmd.IsDark;
        }

        private static void ProcessCommand(LinearInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.Linear;
        }

        private static void ProcessCommand(ClockwiseCircularInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.ClockwiseCircular;
        }

        private static void ProcessCommand(CounterclockwiseCircularInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.CounterclockwiseCircular;
        }

        private static void ProcessCommand(SingleQuadrantModeCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsMultiQuadrant = false;
        }

        private static void ProcessCommand(MultiQuadrantModeCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsMultiQuadrant = true;
        }

        private static void ProcessCommand(CurrentApertureCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.CurrentApertureNumber = cmd.Number;
        }

        private static void ProcessCommand(RegionBeginCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsInsideRegion = true;
        }

        private static void ProcessCommand(RegionEndCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsInsideRegion = false;
            DrawRegion(state);
        }

        private static void DrawRegion(GraphicsState state)
        {
            if (state.CurrentRegion.PointCount == 0)
                return;
            //if (state.IsDarkPolarity)
            //state.GraphObject.FillPolygon(GetBrushColor(state.IsDarkPolarity), state.CurrentRegion.ToArray());
            state.GraphObject.FillPath(GetBrushColor(state.IsDarkPolarity), state.CurrentRegion);
            state.CurrentRegion.Reset();
        }

        private static void ProcessCommand(OperationInterpolateCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            var origPtX = cmd.HasX ? cmd.X : state.CurrentX;
            var origPtY = cmd.HasY ? cmd.Y : state.CurrentY;
            var pt = state.CalcCoords(origPtX, origPtY);
            if (state.IsInsideRegion)
            {
                if (state.Interpolation == InterpolationMode.Linear)
                {
                    state.CurrentRegion.AddLine(state.CalcCoordsForCurrentPoint(), pt);
                }
                else
                {
                    if (!state.IsMultiQuadrant)
                    {
                        var cmdPoint = new CoordinatePair
                        {
                            X = origPtX,
                            Y = origPtY
                        };
                        var beginPoint = state.GetCurrentPoint();
                        var centerCandidates = new CoordinatePair[4];
                        centerCandidates[0] = new CoordinatePair
                        {
                            X = state.CurrentX - cmd.OffsetX,
                            Y = state.CurrentY - cmd.OffsetY
                        };
                        centerCandidates[1] = new CoordinatePair
                        {
                            X = state.CurrentX - cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        centerCandidates[2] = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY - cmd.OffsetY
                        };
                        centerCandidates[3] = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        Rectangle rect;
                        float angleF, sweep;
                        double angle = CalculateAngle(centerCandidates[0], beginPoint, cmdPoint);
                        if (angle != 0.0 && (state.Interpolation == InterpolationMode.ClockwiseCircular)
                            ? (angle > 0.0)
                            : (angle < 0.0))
                        {
                            CalculateArc(centerCandidates[0], beginPoint, cmdPoint, state, out angleF, out sweep, out rect);
                            state.CurrentRegion.AddArc(rect, angleF, sweep);
                            goto end;
                        }
                        angle = CalculateAngle(centerCandidates[1], beginPoint, cmdPoint);
                        if (angle != 0.0 && (state.Interpolation == InterpolationMode.ClockwiseCircular)
                            ? (angle > 0.0)
                            : (angle < 0.0))
                        {
                            CalculateArc(centerCandidates[1], beginPoint, cmdPoint, state, out angleF, out sweep, out rect);
                            state.CurrentRegion.AddArc(rect, angleF, sweep);
                            goto end;
                        }
                        angle = CalculateAngle(centerCandidates[2], beginPoint, cmdPoint);
                        if (angle != 0.0 && (state.Interpolation == InterpolationMode.ClockwiseCircular)
                            ? (angle > 0.0)
                            : (angle < 0.0))
                        {
                            CalculateArc(centerCandidates[2], beginPoint, cmdPoint, state, out angleF, out sweep, out rect);
                            state.CurrentRegion.AddArc(rect, angleF, sweep);
                            goto end;
                        }
                        angle = CalculateAngle(centerCandidates[3], beginPoint, cmdPoint);
                        if (angle != 0.0 && (state.Interpolation == InterpolationMode.ClockwiseCircular)
                            ? (angle > 0.0)
                            : (angle < 0.0))
                        {
                            CalculateArc(centerCandidates[3], beginPoint, cmdPoint, state, out angleF, out sweep, out rect);
                            state.CurrentRegion.AddArc(rect, angleF, sweep);
                            goto end;
                        }
                        end:
                        ;
                    }
                    else
                    {
                        var cmdPoint = new CoordinatePair
                        {
                            X = origPtX,
                            Y = origPtY
                        };
                        var beginPoint = state.GetCurrentPoint();
                        var center = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        Rectangle rect;
                        float angle, sweep;
                        CalculateArc(center, beginPoint, cmdPoint, state, out angle, out sweep, out rect);
                        state.CurrentRegion.AddArc(rect, -angle, sweep);
                    }
                }
                //state.CurrentRegion.AddA
            }
            else
            {
                var currAp = state.Apertures[state.CurrentApertureNumber];
                Pen pen;
                switch (currAp.Template)
                {
                    case "C":
                        pen = new Pen(GetBrushColor(state.IsDarkPolarity), (float)state.ScaleByRenderScale(currAp.Diameter));
                        break;
                    case "R":
                        pen = new Pen(GetBrushColor(state.IsDarkPolarity),
                            (float)state.ScaleByRenderScale(Math.Max(currAp.SizeX, currAp.SizeY)));
                        break;
                    default:
                        pen = new Pen(GetBrushColor(state.IsDarkPolarity), 1);
                        break;
                }
                if (state.Interpolation == InterpolationMode.Linear)
                {
                    var currPt = state.CalcCoordsForCurrentPoint();
                    state.GraphObject.DrawLine(pen, currPt, pt);
                    DrawCurrentAperture(state, state.CurrentX, state.CurrentY);
                    DrawCurrentAperture(state, origPtX, origPtY);
                }
                else
                {
                    if (!state.IsMultiQuadrant)
                    {
                        var cmdPoint = new CoordinatePair
                        {
                            X = origPtX,
                            Y = origPtY
                        };
                        var beginPoint = state.GetCurrentPoint();
                        var centerCandidates = new CoordinatePair[4];
                        centerCandidates[0] = new CoordinatePair
                        {
                            X = state.CurrentX - cmd.OffsetX,
                            Y = state.CurrentY - cmd.OffsetY
                        };
                        centerCandidates[1] = new CoordinatePair
                        {
                            X = state.CurrentX - cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        centerCandidates[2] = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY - cmd.OffsetY
                        };
                        centerCandidates[3] = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        var angle = CalculateAngle(centerCandidates[0], beginPoint, cmdPoint);
                        angle = CalculateAngle(centerCandidates[1], beginPoint, cmdPoint);
                        angle = CalculateAngle(centerCandidates[2], beginPoint, cmdPoint);
                        angle = CalculateAngle(centerCandidates[3], beginPoint, cmdPoint);
                    }
                    else
                    {
                        var cmdPoint = new CoordinatePair
                        {
                            X = origPtX,
                            Y = origPtY
                        };
                        var beginPoint = state.GetCurrentPoint();
                        var center = new CoordinatePair
                        {
                            X = state.CurrentX + cmd.OffsetX,
                            Y = state.CurrentY + cmd.OffsetY
                        };
                        Rectangle rect;
                        float angle, sweep;
                        if (count == 0)
                        {
                            CalculateArc(center, beginPoint, cmdPoint, state, out angle, out sweep, out rect);
                            state.GraphObject.DrawArc(pen, rect, -angle, sweep);
                        }
                        count++;
                    }
                    pen.Dispose();
                    //state.GraphObject.d
                }
            }
            state.CurrentX = origPtX;
            state.CurrentY = origPtY;
        }

        private static int count = 0;

        private static double CalculateAngle(CoordinatePair center, CoordinatePair p1, CoordinatePair p2)
        {
            var vp1 = p1 - center;
            var vp2 = p2 - center;
            var rad = CoordLength(vp1);
            if (Math.Abs(rad - CoordLength(vp2)) > 0.01)
            {
                //invalid arc
                return 0.0;
            }
            var angle1 = Math.Asin((double)vp1.Y / rad);
            if (angle1 == 0.0 && vp1.X < 0)
                angle1 = Math.PI;
            var angle2 = Math.Asin((double)vp2.Y / rad);
            if (angle2 == 0.0 && vp2.X < 0)
                angle2 = Math.PI;
            return angle1 - angle2;
        }

        private static double CoordLength(CoordinatePair c)
        {
            return Math.Sqrt((double)(c.X * c.X) + (double)(c.Y * c.Y));
        }

        private static void CalculateAngle(CoordinatePair center, CoordinatePair p1, CoordinatePair p2, out double radius, out double angle1, out double angle2)
        {
            var vp1 = p1 - center;
            var vp2 = p2 - center;
            radius = Math.Sqrt((double)(vp1.X * vp1.X) + (double)(vp1.Y * vp1.Y));
            angle1 = Math.Asin((double)vp1.Y / radius);
            if (vp1.X < 0)
                angle1 = Math.PI - angle1;
            angle2 = Math.Asin((double)vp2.Y / radius);
            if (vp2.X < 0)
                angle2 = Math.PI - angle2;
        }

        private static void CalculateArc(CoordinatePair center, CoordinatePair p1, CoordinatePair p2, GraphicsState state,
            out float angle, out float sweep, out Rectangle rect)
        {
            double radius, angle1, angle2;
            CalculateAngle(center, p1, p2, out radius, out angle1, out angle2);
            angle2 = angle1 - angle2;
            /*if (angle2 == 0.0)
                angle2 = Math.PI * 2;*/
            if ((state.Interpolation == InterpolationMode.ClockwiseCircular && angle2 < 0) ||
                (state.Interpolation == InterpolationMode.CounterclockwiseCircular && angle2 > 0))
            {
                angle2 = 2 * Math.PI - angle2;
            }
            if (state.Interpolation == InterpolationMode.CounterclockwiseCircular)
                angle2 = -angle2;
            var lowPt = state.CalcCoords(center.X - (decimal)radius, center.Y + (decimal)radius);
            var hiPt = state.CalcCoords(center.X + (decimal)radius, center.Y - (decimal)radius);
            if (lowPt.X == hiPt.X)
                hiPt.X++;
            if (lowPt.Y == hiPt.Y)
                hiPt.Y++;
            rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
            angle = (float)(angle1 * (180.0 / Math.PI));
            sweep = (float)(angle2 * (180.0 / Math.PI));
        }

        private static void ProcessCommand(OperationMoveCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            var origPtX = cmd.HasX ? cmd.X : state.CurrentX;
            var origPtY = cmd.HasY ? cmd.Y : state.CurrentY;
            var pt = state.CalcCoords(origPtX, origPtY);
            if (state.IsInsideRegion)
            {
                DrawRegion(state);
                //state.CurrentRegion.Add(pt);
            }
            else
            {
                if (cmd.HasX)
                    state.CurrentX = cmd.X;
                if (cmd.HasY)
                    state.CurrentY = cmd.Y;
            }

            state.CurrentX = origPtX;
            state.CurrentY = origPtY;
        }

        private static void ProcessCommand(OperationFlashCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            var origPtX = cmd.HasX ? cmd.X : state.CurrentX;
            var origPtY = cmd.HasY ? cmd.Y : state.CurrentY;
            DrawCurrentAperture(state, origPtX, origPtY);
            state.CurrentX = origPtX;
            state.CurrentY = origPtY;
        }

        private static void DrawCurrentAperture(GraphicsState state, decimal x, decimal y)
        {
            var currAp = state.Apertures[state.CurrentApertureNumber];
            if (currAp.Template == "C")
            {
                var halfD = currAp.Diameter * state.Divisor / 2;
                var lowPt = state.CalcCoords(x - halfD, y + halfD);
                var hiPt = state.CalcCoords(x + halfD, y - halfD);
                if (lowPt.X == hiPt.X)
                    hiPt.X++;
                if (lowPt.Y == hiPt.Y)
                    hiPt.Y++;
                var rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
                using (var pen = new Pen(GetBrushColor(state.IsDarkPolarity)))
                {
                    state.GraphObject.DrawEllipse(pen, rect);
                }
                state.GraphObject.FillEllipse(GetBrushColor(state.IsDarkPolarity), rect);
            }
            else if (currAp.Template == "R")
            {
                var halfX = currAp.SizeX * state.Divisor / 2;
                var halfY = currAp.SizeY * state.Divisor / 2;
                var lowPt = state.CalcCoords(x - halfX, y + halfY);
                var hiPt = state.CalcCoords(x + halfX, y - halfY);
                if (lowPt.X == hiPt.X)
                    hiPt.X++;
                if (lowPt.Y == hiPt.Y)
                    hiPt.Y++;
                var rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
                using (var pen = new Pen(GetBrushColor(state.IsDarkPolarity)))
                {
                    state.GraphObject.DrawRectangle(pen, rect);
                }
                state.GraphObject.FillRectangle(GetBrushColor(state.IsDarkPolarity), rect);
            }
            else if (currAp.Template == "P")
            {
                //state.GraphObject.Fi
                var points = new Point[currAp.VerticesCount];

                for (int i = 0; i < currAp.VerticesCount; i++)
                {
                    var angle = i * Math.PI * 2 / currAp.VerticesCount;
                    var currX = x + (decimal)Math.Cos(angle) * currAp.Diameter / 2 * state.Divisor;
                    var currY = y + (decimal)Math.Sin(angle) * currAp.Diameter / 2 * state.Divisor;
                    points[i] = state.CalcCoords(currX, currY);
                }
                using (var pen = new Pen(GetBrushColor(state.IsDarkPolarity)))
                {
                    state.GraphObject.DrawPolygon(pen, points);
                }
                state.GraphObject.FillPolygon(GetBrushColor(state.IsDarkPolarity), points);
            }
            else if (currAp.Template == "O")
            {
                if (currAp.SizeX == currAp.SizeY)
                {
                    var halfX = currAp.SizeX * state.Divisor / 2;
                    var halfY = currAp.SizeY * state.Divisor / 2;
                    var lowPt = state.CalcCoords(x - halfX, y + halfY);
                    var hiPt = state.CalcCoords(x + halfX, y - halfY);
                    if (lowPt.X == hiPt.X)
                        hiPt.X++;
                    if (lowPt.Y == hiPt.Y)
                        hiPt.Y++;
                    var rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
                    using (var pen = new Pen(GetBrushColor(state.IsDarkPolarity)))
                    {
                        state.GraphObject.DrawRectangle(pen, rect);
                    }
                    state.GraphObject.FillRectangle(GetBrushColor(state.IsDarkPolarity), rect);
                }
                else
                {
                    var isRectX = currAp.SizeX > currAp.SizeY;
                    var circleRadius = Math.Min(currAp.SizeX, currAp.SizeY) * state.Divisor / 2;
                    var circleOffset = Math.Abs(currAp.SizeY - currAp.SizeX) * state.Divisor / 2;
                    var circleOffsetX = isRectX ? circleOffset : 0.0m;
                    var circleOffsetY = isRectX ? 0.0m : circleOffset;
                    var c1Low = state.CalcCoords(x - circleOffsetX - circleRadius, y - circleOffsetY + circleRadius);
                    var c1Hi = state.CalcCoords(x - circleOffsetX + circleRadius, y - circleOffsetY - circleRadius);
                    var c1Rect = new Rectangle(c1Low.X, c1Low.Y, c1Hi.X - c1Low.X, Math.Abs(c1Hi.Y - c1Low.Y));

                    var c2Low = state.CalcCoords(x + circleOffsetX - circleRadius, y + circleOffsetY + circleRadius);
                    var c2Hi = state.CalcCoords(x + circleOffsetX + circleRadius, y + circleOffsetY - circleRadius);
                    var c2Rect = new Rectangle(c2Low.X, c2Low.Y, c2Hi.X - c2Low.X, Math.Abs(c2Hi.Y - c2Low.Y));

                    var connLow = state.CalcCoords(x - circleOffsetX - (isRectX ? 0.0m : circleRadius), y + circleOffsetY + (isRectX ? circleRadius : 0.0m));
                    var connHi = state.CalcCoords(x + circleOffsetX + (isRectX ? 0.0m : circleRadius), y - circleOffsetY - (isRectX ? circleRadius : 0.0m));
                    var connRect = new Rectangle(connLow.X, connLow.Y, connHi.X - connLow.X, Math.Abs(connHi.Y - connLow.Y));

                    using (var pen = new Pen(GetBrushColor(state.IsDarkPolarity)))
                    {
                        state.GraphObject.DrawEllipse(pen, c1Rect);
                        state.GraphObject.DrawEllipse(pen, c2Rect);
                        state.GraphObject.DrawRectangle(pen, connRect);
                    }
                    state.GraphObject.FillEllipse(GetBrushColor(state.IsDarkPolarity), c1Rect);
                    state.GraphObject.FillEllipse(GetBrushColor(state.IsDarkPolarity), c2Rect);
                    state.GraphObject.FillRectangle(GetBrushColor(state.IsDarkPolarity), connRect);
                }
            }
            else if (currAp.Template == "M")
            {
                var macro = state.ApertureMacros[currAp.MacroName];
                RenderMacro(macro, state, x, y);
            }
        }

        private static void RenderMacro(ApertureMacroDefinitionCommand cmd, GraphicsState state, decimal x, decimal y)
        {
            foreach (var primitive in cmd.Primitives)
            {
                switch (primitive.Id)
                {
                    case MoireMacroPrimitive.CODE:
                        RenderMoireMacro((MoireMacroPrimitive) primitive, state, x, y);
                        break;
                    case ThermalMacroPrimitive.CODE:
                        RenderThermalMacro((ThermalMacroPrimitive) primitive, state, x, y);
                        break;
                }
            }
        }

        private static void RenderMoireMacro(MoireMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
        {
            var center = new CoordinatePair
            {
                X = x + macro.Center.X * state.Divisor,
                Y = y + macro.Center.Y * state.Divisor
            };
            using (var penCircle = new Pen(GetBrushColor(true), (float)state.ScaleByRenderScale(macro.RingThickness)))
            {
                //state.GraphObject.drawE
                for (int i = 0; i < macro.MaxNumberOfRings; i++)
                {
                    var size = macro.OuterRingDiameter / 2 - macro.RingThickness / 2 - (macro.RingThickness + macro.GapBetweenRings) * i;
                    if (size < 0)
                        break;
                    size *= state.Divisor;
                    var lowPt = state.CalcCoords(center.X - size, center.Y + size);
                    var hiPt = state.CalcCoords(center.X + size, center.Y - size);
                    if (lowPt.X == hiPt.X)
                        hiPt.X++;
                    if (lowPt.Y == hiPt.Y)
                        hiPt.Y++;
                    var rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
                    state.GraphObject.DrawEllipse(penCircle, rect);
                }
            }
            using (var penCross = new Pen(GetBrushColor(true), (float) state.ScaleByRenderScale(macro.CrosshairThickness)))
            {
                var size = macro.CrosshairLength / 2;
                size *= state.Divisor;
                var lowPt = state.CalcCoords(center.X - size, center.Y);
                var hiPt = state.CalcCoords(center.X + size, center.Y);
                state.GraphObject.DrawLine(penCross, lowPt, hiPt);

                lowPt = state.CalcCoords(center.X, center.Y + size);
                hiPt = state.CalcCoords(center.X, center.Y - size);
                state.GraphObject.DrawLine(penCross, lowPt, hiPt);
            }
        }

        private static void RenderThermalMacro(ThermalMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
        {
            var center = new CoordinatePair
            {
                X = x + macro.Center.X * state.Divisor,
                Y = y + macro.Center.Y * state.Divisor
            };
            var thickness = macro.OuterDiameter / 2 - macro.InnerDiameter / 2;
            var size = macro.InnerDiameter / 2 + thickness / 2;
            size *= state.Divisor;
            using (var penCircle = new Pen(GetBrushColor(true), (float) state.ScaleByRenderScale(thickness)))
            {
                var lowPt = state.CalcCoords(center.X - size, center.Y + size);
                var hiPt = state.CalcCoords(center.X + size, center.Y - size);
                if (lowPt.X == hiPt.X)
                    hiPt.X++;
                if (lowPt.Y == hiPt.Y)
                    hiPt.Y++;
                var rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
                state.GraphObject.DrawEllipse(penCircle, rect);
            }
            size = macro.OuterDiameter / 2 + thickness / 2;
            size *= state.Divisor;
            using (var penGap = new Pen(GetBrushColor(false), (float) state.ScaleByRenderScale(macro.GapThickness)))
            {
                var mat = new Matrix();
                var centerPt = state.CalcCoords(center.X, center.Y);
                mat.RotateAt((float) macro.Rotation, centerPt);
                var lowPt = state.CalcCoords(center.X - size, center.Y);
                var hiPt = state.CalcCoords(center.X + size, center.Y);
                var points = new[] {lowPt, hiPt};
                mat.TransformPoints(points);
                state.GraphObject.DrawLine(penGap, points[0], points[1]);

                lowPt = state.CalcCoords(center.X, center.Y + size);
                hiPt = state.CalcCoords(center.X, center.Y - size);
                points = new[] { lowPt, hiPt };
                mat.TransformPoints(points);
                state.GraphObject.DrawLine(penGap, points[0], points[1]);
            }
        }

        private sealed class GraphicsState
        {
            private readonly decimal _renderScale;
            private readonly decimal _renderBorder;
            private readonly decimal _renderOffsetX;
            private readonly decimal _renderOffsetY;

            public GraphicsState(decimal renderScale, decimal renderBorder, decimal renderOffsetX, decimal renderOffsetY)
            {
                _renderScale = renderScale;
                _renderBorder = renderBorder;
                _renderOffsetX = renderOffsetX;
                _renderOffsetY = renderOffsetY;
                CurrentScale = 1;
            }

            public Point CalcCoords(decimal x, decimal y)
            {
                var finalX = (int)Math.Round(((x / Divisor) + _renderOffsetX + _renderBorder) * _renderScale);
                var finalY = (int)Math.Round(((y / Divisor) + _renderOffsetY + _renderBorder) * _renderScale);
                finalY = (int)GraphObject.VisibleClipBounds.Size.Height - finalY;
                return new Point(finalX, finalY);
            }

            public Point CalcCoordsForCurrentPoint()
            {
                return CalcCoords(CurrentX, CurrentY);
            }

            public CoordinatePair GetCurrentPoint()
            {
                return new CoordinatePair
                {
                    X = CurrentX,
                    Y = CurrentY
                };
            }

            public int CalcRelativeCoord(decimal x)
            {
                return (int)Math.Round((x / Divisor) * _renderScale);
            }

            public decimal ScaleByRenderScale(decimal x)
            {
                return x * _renderScale;
            }

            public float CalcRelativeCoordFloat(decimal x)
            {
                return (float)Math.Round((x / Divisor) * _renderScale);
            }

            public int IntPrecision { get; set; }
            public int DecPrecision { get; set; }
            public decimal Divisor { get; set; }

            public decimal CurrentX { get; set; }
            public decimal CurrentY { get; set; }
            public int CurrentApertureNumber { get; set; }
            public InterpolationMode Interpolation { get; set; }
            public bool IsMultiQuadrant { get; set; }

            public bool IsDarkPolarity { get; set; }
            public bool IsMirrorX { get; set; }
            public bool IsMirrorY { get; set; }
            public decimal CurrentRotation { get; set; }
            public decimal CurrentScale { get; set; }

            public bool IsInsideRegion { get; set; }

            public IDictionary<int, ApertureDefinitionCommand> Apertures { get; private set; } = new Dictionary<int, ApertureDefinitionCommand>();
            public IDictionary<string, ApertureMacroDefinitionCommand> ApertureMacros { get; private set; } = new Dictionary<string, ApertureMacroDefinitionCommand>();

            public Graphics GraphObject { get; set; }

            public GraphicsPath CurrentRegion { get; private set; } = new GraphicsPath();
        }

        private enum InterpolationMode
        {
            Linear,
            ClockwiseCircular,
            CounterclockwiseCircular
        }
    }
}
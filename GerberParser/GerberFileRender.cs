using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using GerberParser.Commands;
using GerberParser.Commands.MacroPrimitives;

namespace GerberParser
{
    public class GerberFileRender : IDisposable
    {

        private readonly Brush _activeBrush;
        private readonly Brush _inactiveBrush;
        private readonly Color _bgColor;

        public GerberFileRender()
        {
            _activeBrush = new SolidBrush(Color.FromArgb(100, 0, 128, 0));
            _bgColor = Color.FromArgb(0, 0, 0, 0);
            _inactiveBrush = new SolidBrush(_bgColor);
        }

        public GerberFileRender(Color activeColor, Color inactiveColor)
        {
            _activeBrush = new SolidBrush(activeColor);
            _bgColor = inactiveColor;
            if (_bgColor.A == 255)
                _inactiveBrush = new SolidBrush(_bgColor);
            else
            {
                var color = Color.FromArgb(255, _bgColor.R, _bgColor.G, _bgColor.B);
                _inactiveBrush = new SolidBrush(color);
            }
        }

        private Brush GetBrushColor(bool isSolid)
        {
            return isSolid ? _activeBrush : _inactiveBrush; //Brushes.Green : Brushes.Black;
        }

        private const int MAX_IMG_SIZE = 20000;

        public void CreateImage(GerberFileObject fileObject, decimal scale, string destFileName)
        {
            using (var img = CreateImageBitmap(fileObject, scale))
            {
                //img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                img.Save(destFileName, ImageFormat.Png);
            }
        }

        public Bitmap CreateImageBitmap(GerberFileObject fileObject, decimal scale)
        {
            decimal minX, maxX, minY, maxY;
            GerberFileProcessor.CalculateExtents(fileObject, out minX, out maxX, out minY, out maxY);
            return CreateImageBitmap(fileObject, scale, minX, maxX, minY, maxY);
        }
        public Bitmap CreateImageBitmap(GerberFileObject fileObject, decimal scale, decimal minX, decimal maxX, decimal minY, decimal maxY)
        {

            //var border = fileObject.IsMetric ? 5.0m : 0.2m;
            var border = 0m;

            var offsetX = -minX;
            var offsetY = -minY;

            var state = new GraphicsState(scale, border, offsetX, offsetY);
            state.Divisor = fileObject.Divisor;

            var sizeX = (int)Math.Round((maxX - minX + border * 2) * scale);
            var sizeY = (int)Math.Round((maxY - minY + border * 2) * scale);
            if (sizeY > MAX_IMG_SIZE || sizeX > MAX_IMG_SIZE)
            {
                throw new Exception("ERROR - the image is too large, recude scale.");
            }
            Bitmap img;
            try
            {
                img = new Bitmap(sizeX, sizeY);
            }
            catch (Exception)
            {
                throw new Exception("ERROR - the image is too large, recude scale.");
            }
            var gx = Graphics.FromImage(img);
            gx.Clear(_bgColor);
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
            gx.Dispose();
            return img;
        }

        private void ProcessCommand(FormatStatementCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IntPrecision = cmd.IntegerPositions;
            state.DecPrecision = cmd.DecimalPositions;
        }

        private void ProcessCommand(ApertureDefinitionCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Apertures.Add(cmd.Number, cmd);
        }

        private void ProcessCommand(ApertureMacroDefinitionCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.ApertureMacros.Add(cmd.Name, cmd);
        }

        private void ProcessCommand(PolarityCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsDarkPolarity = cmd.IsDark;
        }

        private void ProcessCommand(LinearInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.Linear;
        }

        private void ProcessCommand(ClockwiseCircularInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.ClockwiseCircular;
        }

        private void ProcessCommand(CounterclockwiseCircularInterpolationCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.Interpolation = InterpolationMode.CounterclockwiseCircular;
        }

        private void ProcessCommand(SingleQuadrantModeCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsMultiQuadrant = false;
        }

        private void ProcessCommand(MultiQuadrantModeCommand cmd, GraphicsState state)
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

        private void ProcessCommand(RegionBeginCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsInsideRegion = true;
        }

        private void ProcessCommand(RegionEndCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            state.IsInsideRegion = false;
            DrawRegion(state);
        }

        private void DrawRegion(GraphicsState state)
        {
            if (state.CurrentRegion.PointCount == 0)
                return;
            //if (state.IsDarkPolarity)
            //state.GraphObject.FillPolygon(GetBrushColor(state.IsDarkPolarity), state.CurrentRegion.ToArray());
            state.GraphObject.FillPath(GetBrushColor(state.IsDarkPolarity), state.CurrentRegion);
            state.CurrentRegion.Reset();
        }

        private Pen CreatePenForCurrentAperture(GraphicsState state)
        {
            var currAp = state.Apertures[state.CurrentApertureNumber];
            Pen pen;
            switch (currAp.Template)
            {
                case "C":
                case "P":
                    pen = new Pen(GetBrushColor(state.IsDarkPolarity), (float)state.ScaleByRenderScale(currAp.Diameter));
                    break;
                case "R":
                case "O":
                    pen = new Pen(GetBrushColor(state.IsDarkPolarity),
                        (float)state.ScaleByRenderScale(Math.Max(currAp.SizeX, currAp.SizeY)));
                    break;
                default:
                    pen = new Pen(GetBrushColor(state.IsDarkPolarity), 1);
                    break;
            }
            return pen;
        }

        private CoordinatePair FindCenterPoint(CoordinatePair beginPoint, CoordinatePair cmdPoint, OperationInterpolateCommand cmd, InterpolationMode mode)
        {
            var centerCandidates = new CoordinatePair[4];
            centerCandidates[0] = new CoordinatePair
            {
                X = beginPoint.X - cmd.OffsetX,
                Y = beginPoint.Y - cmd.OffsetY
            };
            centerCandidates[1] = new CoordinatePair
            {
                X = beginPoint.X - cmd.OffsetX,
                Y = beginPoint.Y + cmd.OffsetY
            };
            centerCandidates[2] = new CoordinatePair
            {
                X = beginPoint.X + cmd.OffsetX,
                Y = beginPoint.Y - cmd.OffsetY
            };
            centerCandidates[3] = new CoordinatePair
            {
                X = beginPoint.X + cmd.OffsetX,
                Y = beginPoint.Y + cmd.OffsetY
            };
            foreach (var candidate in centerCandidates)
            {
                double angle = CalculateAngle(candidate, beginPoint, cmdPoint);
                if (angle != 0.0 && (mode == InterpolationMode.ClockwiseCircular)
                    ? (angle > 0.0)
                    : (angle < 0.0) && Math.Abs(angle) <= Math.PI / 2)
                {
                    return candidate;
                }
            }
            return null;
        }

        private void ProcessCommand(OperationInterpolateCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            var origPtX = cmd.HasX ? cmd.X : state.CurrentX;
            var origPtY = cmd.HasY ? cmd.Y : state.CurrentY;
            var pt = state.CalcCoords(origPtX, origPtY);
            if (state.Interpolation == InterpolationMode.Linear)
            {
                var currPt = state.CalcCoordsForCurrentPoint();
                if (state.IsInsideRegion)
                {
                    state.CurrentRegion.AddLine(state.CalcCoordsForCurrentPoint(), pt);
                }
                else
                {
                    using (var pen = CreatePenForCurrentAperture(state))
                    {
                        state.GraphObject.DrawLine(pen, currPt, pt);
                        DrawCurrentAperture(state, state.CurrentX, state.CurrentY);
                        DrawCurrentAperture(state, origPtX, origPtY);
                    }
                }
            }
            else
            {
                CoordinatePair center;
                Rectangle rect;
                float angleF, sweep;
                var cmdPoint = new CoordinatePair
                {
                    X = origPtX,
                    Y = origPtY
                };
                var beginPoint = state.GetCurrentPoint();
                if (!state.IsMultiQuadrant)
                {
                    center = FindCenterPoint(beginPoint, cmdPoint, cmd, state.Interpolation);
                }
                else
                {
                    center = new CoordinatePair
                    {
                        /*X = state.CurrentX + (state.Interpolation == InterpolationMode.ClockwiseCircular ? cmd.OffsetX : -cmd.OffsetX),
                        Y = state.CurrentY + (state.Interpolation == InterpolationMode.ClockwiseCircular ? cmd.OffsetY : -cmd.OffsetY)*/
                        X = state.CurrentX + cmd.OffsetX,
                        Y = state.CurrentY + cmd.OffsetY
                    };
                }
                CalculateArc(center, beginPoint, cmdPoint, state, out angleF, out sweep, out rect);
                if (sweep != 0 && Math.Abs(sweep) < 1f)
                {
                    sweep = 1f * Math.Sign(sweep);
                }
                if (sweep == 0)
                    sweep = state.Interpolation == InterpolationMode.ClockwiseCircular ? 360.0f : -360.0f;
                if (state.IsInsideRegion)
                {
                    state.CurrentRegion.AddArc(rect, angleF, sweep);
                }
                else
                {
                    using (var pen = CreatePenForCurrentAperture(state))
                    {
                        state.GraphObject.DrawArc(pen, rect, angleF, sweep);
                    }
                }
            }
            state.CurrentX = origPtX;
            state.CurrentY = origPtY;
        }

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
            double radius, angle1, angle2;
            CalculateAngle(center, p1, p2, out radius, out angle1, out angle2);
            /*var angle1 = Math.Asin((double)vp1.Y / rad);
            if (angle1 == 0.0 && vp1.X < 0)
                angle1 = Math.PI;
            var angle2 = Math.Asin((double)vp2.Y / rad);
            if (angle2 == 0.0 && vp2.X < 0)
                angle2 = Math.PI;*/
            return angle1 - angle2;
        }

        private static double CoordLength(CoordinatePair c)
        {
            return Math.Sqrt((double)(c.X * c.X) + (double)(c.Y * c.Y));
        }

        private static double Saturate(double val, double min, double max)
        {
            return val < min ? min : (val > max ? max : val);
        }

        private static double CalculateAngle(CoordinatePair vec)
        {
            var x = (double)vec.X;
            var y = (double)vec.Y;
            /*if (x > 0)
            {
                if (y >= 0)
                    return Math.Atan(y / x);
                else
                    return Math.Atan(y / x) + Math.PI * 2;
            }
            else if (x == 0.0)
            {
                if (y > 0)
                    return Math.PI / 2;
                else if (y == 0)
                    return Math.PI * 2;
                else
                    return 3 * Math.PI / 2;
            }
            else
            {
                return Math.Atan(y / x) + Math.PI;
            }*/
            var res = Math.Atan2(y, x);
            if (res < 0)
                res += Math.PI * 2;
            return res;
        }

        private static void CalculateAngle(CoordinatePair center, CoordinatePair p1, CoordinatePair p2, out double radius, out double angle1, out double angle2)
        {
            var vp1 = p1 - center;
            var vp2 = p2 - center;
            radius = Math.Sqrt((double)(vp1.X * vp1.X) + (double)(vp1.Y * vp1.Y));
            var rad2 = Math.Sqrt((double)(vp2.X * vp2.X) + (double)(vp2.Y * vp2.Y));
            radius = Math.Max(radius, rad2);



            /*var sin1 = Saturate((double)vp1.Y / radius, -1.0, 1.0);
            var cos1 = Saturate((double)vp1.X / radius, -1.0, 1.0);
            var asin1 = Math.Asin(sin1);
            var acos1 = Math.Acos(cos1);
            angle1 = asin1;
            if (angle1 <= 0)
                angle1 = acos1;*/
            angle1 = CalculateAngle(vp1);
            angle1 = Math.PI * 2 - angle1;

            /*var sin2 = Saturate((double)vp2.Y / radius, -1.0, 1.0);
            var cos2 = Saturate((double)vp2.X / radius, -1.0, 1.0);
            var asin2 = Math.Asin(sin2);
            var acos2 = Math.Acos(cos2);
            angle2 = asin2;
            if (angle2 <= 0)
                angle2 = acos2;*/
            angle2 = CalculateAngle(vp2);
            angle2 = Math.PI * 2 - angle2;
            /*angle1 = Math.Asin((double)vp1.Y / radius);
            if (vp1.X < 0)
                angle1 = Math.PI - angle1;
            if (angle1 < 0)
                angle1 += 2 * Math.PI;
            angle2 = Math.Asin((double)vp2.Y / radius);
            if (vp2.X < 0)
                angle2 = Math.PI - angle2;
            if (angle2 < 0)
                angle2 += 2 * Math.PI;*/

            var a1 = angle1 * 180.0 / Math.PI;
            var a2 = angle2 * 180.0 / Math.PI;
            Debug.WriteLine($"vp1({vp1.X},{vp1.Y}), angle = {a1}");
            Debug.WriteLine($"vp2({vp2.X},{vp2.Y}), angle = {a2}");
        }

        private static void CalculateArc(CoordinatePair center, CoordinatePair p1, CoordinatePair p2, GraphicsState state,
            out float angle, out float sweep, out Rectangle rect)
        {
            double radius, angle1, angle2;
            CalculateAngle(center, p1, p2, out radius, out angle1, out angle2);
            /*if (angle2 == 0.0 && state.Interpolation == InterpolationMode.ClockwiseCircular)
                angle2 = Math.PI * 2;
            if (angle1 == 0.0 && state.Interpolation == InterpolationMode.CounterclockwiseCircular)
                angle1 = Math.PI * 2;*/
            if (state.Interpolation == InterpolationMode.ClockwiseCircular && angle2 < angle1)
                angle2 += Math.PI * 2;
            else if (state.Interpolation == InterpolationMode.CounterclockwiseCircular && angle2 > angle1)
                angle2 -= Math.PI * 2;
            angle2 = angle2 - angle1;
            if ((state.Interpolation == InterpolationMode.ClockwiseCircular && angle2 < 0) ||
                (state.Interpolation == InterpolationMode.CounterclockwiseCircular && angle2 > 0))
            {
                //angle2 = -(2 * Math.PI - angle2);
                1.ToString();
            }
            /*if (state.Interpolation == InterpolationMode.ClockwiseCircular && state.IsMultiQuadrant)
                angle2 = (2 * Math.PI - angle2);*/
            var a1 = angle1 * 180.0 / Math.PI;
            var a2 = angle2 * 180.0 / Math.PI;
            Debug.WriteLine($"start {a1} sweep {a2} int mode {state.Interpolation}");
            var lowPt = state.CalcCoords(center.X - (decimal)radius, center.Y + (decimal)radius);
            var hiPt = state.CalcCoords(center.X + (decimal)radius, center.Y - (decimal)radius);
            if (lowPt.X == hiPt.X)
                hiPt.X++;
            if (lowPt.Y == hiPt.Y)
                hiPt.Y++;
            rect = new Rectangle(lowPt.X, lowPt.Y, hiPt.X - lowPt.X, Math.Abs(hiPt.Y - lowPt.Y));
            angle = (float)(angle1 * (180.0 / Math.PI));
            sweep = (float)(angle2 * (180.0 / Math.PI));
            Debug.WriteLine($"angle {angle}, sweep {sweep}");
        }

        private void ProcessCommand(OperationMoveCommand cmd, GraphicsState state)
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

        private void ProcessCommand(OperationFlashCommand cmd, GraphicsState state)
        {
            if (cmd == null)
                return;
            var origPtX = cmd.HasX ? cmd.X : state.CurrentX;
            var origPtY = cmd.HasY ? cmd.Y : state.CurrentY;
            DrawCurrentAperture(state, origPtX, origPtY);
            state.CurrentX = origPtX;
            state.CurrentY = origPtY;
        }

        private void DrawCurrentAperture(GraphicsState state, decimal x, decimal y)
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

        private void RenderMacro(ApertureMacroDefinitionCommand cmd, GraphicsState state, decimal x, decimal y)
        {
            foreach (var primitive in cmd.Primitives)
            {
                switch (primitive.Id)
                {
                    case CircleMacroPrimitive.CODE:
                        RenderCircleMacro((CircleMacroPrimitive)primitive, state, x, y);
                        break;
                    case MoireMacroPrimitive.CODE:
                        RenderMoireMacro((MoireMacroPrimitive) primitive, state, x, y);
                        break;
                    case ThermalMacroPrimitive.CODE:
                        RenderThermalMacro((ThermalMacroPrimitive) primitive, state, x, y);
                        break;
                    case OutlineMacroPrimitive.CODE:
                        RenderOutlineMacro((OutlineMacroPrimitive) primitive, state, x, y);
                        break;
                    case CenterLineMacroPrimitive.CODE:
                        RenderCenterLineMarco((CenterLineMacroPrimitive) primitive, state, x, y);
                        break;
                    default:
                        Console.WriteLine($"ERROR - invalid primitive ID {primitive.Id}");
                        break;
                }
            }
        }

        private void RenderCircleMacro(CircleMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
        {
            var center = new CoordinatePair
            {
                X = x + macro.Center.X * state.Divisor,
                Y = y + macro.Center.Y * state.Divisor
            };
            var diameter = state.CalcRelativeCoord(macro.Diameter * state.Divisor);
            var centerDraw = state.CalcCoords(center.X, center.Y);

            var rect = new Rectangle(-diameter / 2, -diameter / 2, diameter, diameter);
            var mat = state.GraphObject.Transform;
            state.GraphObject.TranslateTransform(centerDraw.X, centerDraw.Y);
            state.GraphObject.RotateTransform((float)macro.Rotation);
            state.GraphObject.FillEllipse(GetBrushColor(state.IsDarkPolarity), rect);
            state.GraphObject.Transform = mat;
        }

        private void RenderCenterLineMarco(CenterLineMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
        {
            var center = new CoordinatePair
            {
                X = x + macro.Center.X * state.Divisor,
                Y = y + macro.Center.Y * state.Divisor
            };
            var sizeX = state.CalcRelativeCoord(macro.Width * state.Divisor);
            var sizeY = state.CalcRelativeCoord(macro.Height * state.Divisor);
            var centerDraw = state.CalcCoords(center.X, center.Y);
            

            var rect = new Rectangle(-sizeX/2, -sizeY/2, sizeX, sizeY);
            var mat = state.GraphObject.Transform;
            state.GraphObject.TranslateTransform(centerDraw.X, centerDraw.Y);
            state.GraphObject.RotateTransform((float) macro.Rotation);
            state.GraphObject.FillRectangle(GetBrushColor(state.IsDarkPolarity), rect);
            state.GraphObject.Transform = mat;
        }

        private void RenderMoireMacro(MoireMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
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

        private void RenderThermalMacro(ThermalMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
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

        private void RenderOutlineMacro(OutlineMacroPrimitive macro, GraphicsState state, decimal x, decimal y)
        {
            var points = new List<Point>();
            foreach (var pt in macro.Points)
            {
                points.Add(state.CalcCoords(x + pt.X * state.Divisor, y + pt.Y * state.Divisor));
            }

            state.GraphObject.FillPolygon(GetBrushColor(true), points.ToArray());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _activeBrush.Dispose();
                _inactiveBrush.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                IsDarkPolarity = true;
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
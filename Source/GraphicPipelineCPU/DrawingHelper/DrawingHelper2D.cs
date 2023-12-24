using System;
using System.Collections.Generic;
using System.Drawing;
using GraphicMinimal;
using BitmapHelper;
using GraphicPipelineCPU.Textures;
using GraphicPipelineCPU.Rasterizer;
using GraphicGlobal;
using GraphicGlobal.Rasterizer2DFunctions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GraphicPipelineCPU.DrawingHelper
{
    class DrawingHelper2D
    {
        private PropertysForDrawing prop = null;

        public DrawingHelper2D(PropertysForDrawing prop)
        {
            this.prop = prop;
        }

        private void DrawTriangle2D(Vertex2D p1, Vertex2D p2, Vertex2D p3, Color color)
        {
            DrawTriangle2D(ViewPortTransformation(p1), ViewPortTransformation(p2), ViewPortTransformation(p3), (_) => color);
        }

        public Vector2D ViewPortTransformation(Vector2D v)
        {
            v = Matrix4x4.MultPosition(prop.ModelviewMatrix, new Vector3D(v.X, v.Y, 0)).XY;
            return prop.ViewPort.TransformIntoViewPort(new Vector2D(v.X / (float)prop.DrawingArea.Width, v.Y / (float)prop.DrawingArea.Height));
        }

        private float TransformLength(float length)
        {
            var p1 = Matrix4x4.MultPosition(prop.ModelviewMatrix, new Vector3D(0, 0, 0)).XY;
            var p2 = Matrix4x4.MultPosition(prop.ModelviewMatrix, new Vector3D(length, 0, 0)).XY;

            return (p2 - p1).Length();
        }

        private Vertex2D ViewPortTransformation(Vertex2D v)
        {
            return new Vertex2D(ViewPortTransformation(v.Position), v.Textcoord);
        }

        private void DrawTriangle2D(Vertex2D p1, Vertex2D p2, Vertex2D p3, ColorTexture texture)
        {
            DrawTriangle2D(ViewPortTransformation(p1), ViewPortTransformation(p2), ViewPortTransformation(p3), (p) => GetColorFromImage(texture, p));
        }

        private Color GetColorFromImage(ColorTexture texture, Vertex2D pp)
        {
            Color color = texture.TextureMappingPoint(prop.Deck0.TextureMode, pp.Textcoord.X, pp.Textcoord.Y);

            //colorFactor einbringen
            color = color.MultWithBlendFactor(prop.CurrentColor);

            return color;
        }


        private void DrawTriangle2D(Vertex2D p1, Vertex2D p2, Vertex2D p3, Func<Vertex2D, Color> colorFunc)
        {
            TriangleRasterizer.DrawWindowSpaceTriangle(p1.Position, p2.Position, p3.Position, (w0, w1, w2) =>
            {
                Vector2D textcood = w0 * p1.Textcoord + w1 * p2.Textcoord + w2 * p3.Textcoord;
                if (Single.IsNaN(textcood.X) || Single.IsNaN(textcood.Y))
                    textcood = p1.Textcoord;

                Vertex2D pp = new Vertex2D(w0 * p1.Position + w1 * p2.Position + w2 * p3.Position, textcood);//Interpolierter Vertex

                int x = (int)pp.Position.X;
                int y = (int)pp.Position.Y;
                if (x >= 0 && x < prop.Buffer.Width && y >= 0 && y < prop.Buffer.Height) DrawSinglePixel(x, y, pp, colorFunc);

            }, null);
        }

        private void DrawSinglePixel(int x, int y, Vertex2D pp, Func<Vertex2D, Color> colorFunc)
        {
            if (prop.IsScissorEnabled == false || (prop.IsScissorEnabled && x >= prop.ScissorRectangle.Left && x < prop.ScissorRectangle.Right && y >= prop.ScissorRectangle.Top && y < prop.ScissorRectangle.Bottom))
            {
                if (prop.DepthTestingIsEnabled)                                 // Dephtest
                {
                    if (prop.ZValue2DTransformed < 0 || prop.ZValue2DTransformed > 1) return;         // Pixel liegt nicht im Sichtbereich
                    if (prop.Buffer.Depth[x, y] < prop.ZValue2DTransformed) return;        // Ein anderer Pixel liegt bereits da
                }

                Color color = colorFunc(pp);

                if (prop.Discard100Transparent && color.A < 2) return;

                if (prop.BlendingIsEnabled)
                {
                    prop.Buffer.Color[x,y] = ColorHelper.ColorAlphaBlending(prop.Buffer.Color[x,y], color, color.A / 255f);
                }
                else
                {
                    prop.Buffer.Color[x,y] = color;
                }

                if (prop.WritingToDepthBuffer && prop.Buffer.Depth != null)
                    prop.Buffer.Depth[x, y] = prop.ZValue2DTransformed;
            }
        }

        public void DrawLineWithTwoTriangles(Pen pen, Vector2D p1, Vector2D p2)
        {
            Vector2D r = (p2 - p1).Normalize();
            Vector2D n = -r.Spin90() / 2 * pen.Width;



            p1 = new Vector2D(p1.X - 0.6f, p1.Y);
            p2 = new Vector2D(p2.X - 0.6f, p2.Y);

            this.DrawTriangle2D(
                new Vertex2D(p1 - n, new Vector2D(0, 0)),       //Links Oben
                new Vertex2D(p1 + n, new Vector2D(1, 0)),   //Rechts oben
                new Vertex2D(p2 + n, new Vector2D(1, 1)),   //Rechts unten
                pen.Color);

            this.DrawTriangle2D(
                new Vertex2D(p2 + n, new Vector2D(1, 1)),   //Rechts unten
                new Vertex2D(p2 - n, new Vector2D(0, 1)),       //Links unten
                new Vertex2D(p1 - n, new Vector2D(0, 0)),       //Links Oben
                pen.Color);
        }

        private void DrawLineWithLineRasterizer(Pen pen, Vector2D p1, Vector2D p2)
        {
            p1 = ViewPortTransformation(p1);
            p2 = ViewPortTransformation(p2);

            LineRasterizer.DrawLine(new Point((int)(p1.X+0.5f), (int)(p1.Y+0.5f)), new Point((int)(p2.X + 0.5f), (int)(p2.Y + 0.5f)), (pix, f) =>
            {
                DrawPixel(new Vector2D(pix.X, pix.Y), pen.Color, pen.Width);
            });
        }

        public void DrawPixel(Vector2D pos, Color color, float size)
        {
            for (int x=0;x<size;x++)
                for (int y=0; y<size;y++)
                {
                    int xi = (int)(pos.X + x - size / 2);
                    int yi = (int)(pos.Y + y - size / 2);
                    if (xi >= 0 && xi < prop.Buffer.Width && yi >= 0 && yi < prop.Buffer.Height)
                    {
                        prop.Buffer.Color[xi,yi] = color;
                    }
                }
        }

        private static SizeF singleLetterSize = new SizeF(0, 0);
        public Size GetStringSize(float size, string text)
        {
            //Mit diesem Algorithmus hier unten kann man die größe Ausmessen, um erstmal zu sehen, wie groß ein einzelner Buchstabe von einer Schriftart überhaupt ist.
            if (singleLetterSize.Width == 0)
            {
                Bitmap image = BitmapHelp.GetBitmapText("WWww", size, Color.Black, Color.White);
                Rectangle reci = BitmapHelp.SearchRectangleInBitmap(image, Color.White);
                singleLetterSize = new SizeF(reci.Width / 4.0f / size, reci.Height / size);//4.. Länge des Textes "WWww"
            }

            return new Size((int)(singleLetterSize.Width * text.Length * size), (int)(singleLetterSize.Height * size * 1.3f));
        }

        public void DrawString(float x, float y, Color color, float size, string text)
        {
            var pos = ViewPortTransformation(new Vector2D(x, y));
            x = pos.X;
            y = pos.Y;

            Bitmap image = BitmapHelp.GetBitmapText(text, size, color, Color.White);
            Rectangle reci = BitmapHelp.SearchRectangleInBitmap(image, Color.White);

            for (int xi = reci.Left; xi <= reci.Right; xi++)
                for (int yi = reci.Top; yi <= reci.Bottom; yi++)
                {
                    if (PixelHelper.CompareTwoColors(image.GetPixel(xi, yi), Color.White) == false)
                        DrawPixel(new Vector2D(x + xi - reci.Left, y + yi - reci.Top), color, 1);
                }
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            DrawLineWithLineRasterizer(pen, new Vector2D(x, y), new Vector2D(x + width, y));
            DrawLineWithLineRasterizer(pen, new Vector2D(x + width, y), new Vector2D(x + width, y + height));
            DrawLineWithLineRasterizer(pen, new Vector2D(x + width, y + height), new Vector2D(x, y + height));
            DrawLineWithLineRasterizer(pen, new Vector2D(x, y), new Vector2D(x, y + height));
        }

        public void DrawPolygon(Pen pen, List<Vector2D> points)
        {
            for (int i = 0; i < points.Count; i++)
                DrawLineWithLineRasterizer(pen, points[i], points[(i + 1) % points.Count]);
        }

        public void DrawCircle(Pen pen, Vector2D pos, int radius)
        {
            pos = ViewPortTransformation(pos);
            radius = (int)TransformLength(radius);

            ShapeDrawer.DrawCircle(pos, radius, (p) => DrawPixel(new Vector2D(p.X, p.Y), pen.Color, pen.Width));
        }

        //Quelle: http://stackoverflow.com/questions/1201200/fast-algorithm-for-drawing-filled-circles
        public void DrawFillCircle(Color color, Vector2D pos, int radius)
        {
            pos = ViewPortTransformation(pos);
            radius = (int)TransformLength(radius);

            ShapeDrawer.DrawFillCircle(pos, radius, (p) => DrawPixel(new Vector2D(p.X, p.Y), color, 1));
        }

        public void DrawCircleArc(Pen pen, Vector2D pos, int radius, float startAngle, float endAngle, bool withBorderLines)
        {
            pos = ViewPortTransformation(pos);
            radius = (int)TransformLength(radius);

            CircleArcDrawer.DrawCircleArc(pos, radius, startAngle, endAngle, withBorderLines, (p) => DrawPixel(p, pen.Color, pen.Width));
        }

        public void DrawFillCircleArc(Color color, Vector2D pos, int radius, float startAngle, float endAngle)
        {
            pos = ViewPortTransformation(pos);
            radius = (int)TransformLength(radius);

            CircleArcDrawer.DrawFillCircleArc(pos, radius, startAngle, endAngle, (p) => DrawPixel(new Vector2D(p.X, p.Y), color, 1));
        }

        public void DrawImage(ColorTexture texture, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight)
        {
            Size tex = texture.GetSize();
            DrawTriangle2D(new Vertex2D(x, y, sourceX / (float)tex.Width, sourceY / (float)tex.Height),
                           new Vertex2D(x + width, y, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height),
                           new Vertex2D(x, y + height, sourceX / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height), texture);

            DrawTriangle2D(new Vertex2D(x + width, y + height, (sourceX + sourceWidth) / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height),
                           new Vertex2D(x + width, y, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height),
                           new Vertex2D(x, y + height, sourceX / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height), texture);
        }

        public void DrawImage(ColorTexture texture, int x, int y, int width, int height, int sourceX, int sourceY, int sourceWidth, int sourceHeight, float zAngle, float yAngle)
        {
            Size tex = texture.GetSize();

            Vector2D center = new Vector2D(x, y);
            Vector2D P1 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P2 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P3 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y + height / 2), yAngle), zAngle);
            Vector2D P4 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y + height / 2), yAngle), zAngle);

            DrawTriangle2D(new Vertex2D(P1.X, P1.Y, sourceX / (float)tex.Width, sourceY / (float)tex.Height), new Vertex2D(P2.X, P2.Y, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height), new Vertex2D(P3.X, P3.Y, sourceX / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height), texture);
            DrawTriangle2D(new Vertex2D(P4.X, P4.Y, (sourceX + sourceWidth) / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height), new Vertex2D(P3.X, P3.Y, sourceX / (float)tex.Width, (sourceY + sourceHeight) / (float)tex.Height), new Vertex2D(P2.X, P2.Y, (sourceX + sourceWidth) / (float)tex.Width, sourceY / (float)tex.Height), texture);
        }

        public void DrawFillRectangle(ColorTexture texture, float x, float y, float width, float height)
        {
            DrawTriangle2D(new Vertex2D(x, y, 0, 0), new Vertex2D(x + width, y, 1, 0), new Vertex2D(x, y + height, 0, 1), texture);
            DrawTriangle2D(new Vertex2D(x + width, y + height, 1, 1), new Vertex2D(x, y + height, 0, 1), new Vertex2D(x + width, y, 1, 0), texture);
        }

        public void DrawFillRectangle(ColorTexture texture, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Vector2D center = new Vector2D(x, y);
            Vector2D P1 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x - width / 2, y - height / 2), angle);
            Vector2D P2 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x + width / 2, y - height / 2), angle);
            Vector2D P3 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x - width / 2, y + height / 2), angle);
            Vector2D P4 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x + width / 2, y + height / 2), angle);

            DrawTriangle2D(new Vertex2D(P1.X, P1.Y, 0, 0), new Vertex2D(P2.X, P2.Y, 1, 0), new Vertex2D(P3.X, P3.Y, 0, 1), texture);
            DrawTriangle2D(new Vertex2D(P4.X, P4.Y, 1, 1), new Vertex2D(P3.X, P3.Y, 0, 1), new Vertex2D(P2.X, P2.Y, 1, 0), texture);
        }

        public void DrawFillRectangle(ColorTexture texture, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Vector2D center = new Vector2D(x, y);
            Vector2D P1 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P2 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P3 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y + height / 2), yAngle), zAngle);
            Vector2D P4 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y + height / 2), yAngle), zAngle);

            DrawTriangle2D(new Vertex2D(P1.X, P1.Y, 0, 0), new Vertex2D(P2.X, P2.Y, 1, 0), new Vertex2D(P3.X, P3.Y, 0, 1), texture);
            DrawTriangle2D(new Vertex2D(P4.X, P4.Y, 1, 1), new Vertex2D(P3.X, P3.Y, 0, 1), new Vertex2D(P2.X, P2.Y, 1, 0), texture);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height)
        {
            DrawTriangle2D(new Vertex2D(x, y, 0, 0), new Vertex2D(x + width, y, 1, 0), new Vertex2D(x, y + height, 0, 1), color);
            DrawTriangle2D(new Vertex2D(x + width, y + height, 1, 1), new Vertex2D(x, y + height, 0, 1), new Vertex2D(x + width, y, 1, 0), color);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float angle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Vector2D center = new Vector2D(x, y);

            Vector2D P1 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x - width / 2, y - height / 2), angle);
            Vector2D P2 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x + width / 2, y - height / 2), angle);
            Vector2D P3 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x - width / 2, y + height / 2), angle);
            Vector2D P4 = Vector2D.RotatePointAroundPivotPoint(center, new Vector2D(x + width / 2, y + height / 2), angle);

            DrawTriangle2D(new Vertex2D(P1.X, P1.Y, 0, 0), new Vertex2D(P2.X, P2.Y, 1, 0), new Vertex2D(P3.X, P3.Y, 0, 1), color);
            DrawTriangle2D(new Vertex2D(P4.X, P4.Y, 1, 1), new Vertex2D(P3.X, P3.Y, 0, 1), new Vertex2D(P2.X, P2.Y, 1, 0), color);
        }

        public void DrawFillRectangle(Color color, float x, float y, float width, float height, float zAngle, float yAngle)//x,y liegen in der Mitte, angle geht von 0 bis 360
        {
            Vector2D center = new Vector2D(x, y);
            Vector2D P1 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P2 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y - height / 2), yAngle), zAngle);
            Vector2D P3 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x - width / 2, y + height / 2), yAngle), zAngle);
            Vector2D P4 = Vector2D.RotatePointAroundPivotPoint(center, Vector2D.RotatePointAboutYAxis(x, new Vector2D(x + width / 2, y + height / 2), yAngle), zAngle);

            DrawTriangle2D(new Vertex2D(P1.X, P1.Y, 0, 0), new Vertex2D(P2.X, P2.Y, 1, 0), new Vertex2D(P3.X, P3.Y, 0, 1), color);
            DrawTriangle2D(new Vertex2D(P4.X, P4.Y, 1, 1), new Vertex2D(P3.X, P3.Y, 0, 1), new Vertex2D(P2.X, P2.Y, 1, 0), color);
        }

        public void DrawFillPolygon(ColorTexture texture, List<Triangle2D> triangleList)
        {
            foreach (Triangle2D triangle in triangleList)
            {
                DrawTriangle2D(triangle.P1, triangle.P2, triangle.P3, texture);
            }
        }

        public void DrawFillPolygon(Color color, List<Triangle2D> triangleList)
        {
            foreach (Triangle2D triangle in triangleList)
            {
                DrawTriangle2D(triangle.P1, triangle.P2, triangle.P3, color);
            }
        }

        public void DrawSprite(ColorTexture texture, int xCount, int yCount, int xBild, int yBild, int x, int y, int width, int height)
        {
            float xf = 1.0f / xCount, yf = 1.0f / yCount;
            DrawTriangle2D(new Vertex2D(x, y, xBild * xf + 0.01f, yBild * yf + 0.01f),
                           new Vertex2D(x + width, y, (xBild +1)* xf - 0.01f, yBild * yf + 0.01f),
                           new Vertex2D(x, y + height, xBild * xf + 0.01f, (yBild+1) * yf - 0.01f), texture);
            DrawTriangle2D(new Vertex2D(x + width, y + height, (xBild+1) * xf - 0.01f, (yBild+1) * yf - 0.01f),
                           new Vertex2D(x + width, y, (xBild+1) * xf - 0.01f, yBild * yf + 0.01f),
                           new Vertex2D(x, y + height, xBild * xf + 0.01f, (yBild+1) * yf - 0.01f), texture);
        }
    }
}

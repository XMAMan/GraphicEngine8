using GraphicMinimal;
using System;
using System.Drawing;

namespace GraphicPipelineCPU.Rasterizer
{
    class TriangleRasterizer
    {
        public delegate void PixelCenterCallBack(float w0, float w1, float w2); //w0,w1,w2 sind Byzentrische Koordinaten von der Pixelmitte
        public delegate void PixelCallBack(Point pixel); //Wird für jeden Pixel aufgerufen, dessen Pixelmitte im Dreieck liegt

        //Triangle-Rasterregel: Pixelmitte muss im Dreieck liegen oder, wenn vorhanden, auf der Top-Kante oder auf der Left-Kante
        //https://docs.microsoft.com/en-us/windows/win32/direct3d11/d3d10-graphics-programming-guide-rasterizer-stage-rules
        //https://www.scratchapixel.com/lessons/3d-basic-rendering/rasterization-practical-implementation/rasterization-stage
        public static void DrawWindowSpaceTriangle(Vector2D p0,  Vector2D p1, Vector2D p2, PixelCenterCallBack pixelCenterCallBack, PixelCallBack pixelEdgesCallBack)
        {
            //1. Bounding-Rechteck bestimmen
            Rectangle rec = GetBoundingRectangle(p0, p1, p2);

            //2. Daten für die Top-Left-Regel
            Vector2D edge0 = p2 - p1;
            Vector2D edge1 = p0 - p2;
            Vector2D edge2 = p1 - p0;

            //3. Schauen ob das Dreieck Clockwise liegt oder nicht
            bool isCounterClockwise = IsCounterClockwise(p0, p1, p2);

            //4. Für alle Pixel aus dem Bounding-Rechteck mit dem Edge-Test bestimmen, ob die Pixel-Mitte im Dreieck liegt
            //   oder die Pixelmitte auf der oberen horizontalen oder linken Kante liegt
            float area = EdgeFunction(p0, p1, p2);
            for (int x = rec.X; x <= rec.Right; x++)
                for (int y = rec.Y; y <= rec.Bottom; y++)
                {
                    Vector2D p = new Vector2D(x + 0.5f, y + 0.5f); //Pixelcenter
                    float w0 = EdgeFunction(p1, p2, p);
                    float w1 = EdgeFunction(p2, p0, p);
                    float w2 = EdgeFunction(p0, p1, p);

                    //Top-Left-Regel
                    bool overlaps = true;
                    if (isCounterClockwise) //Dreieck liegt CCW
                    {
                        //IsZero(w0)        ->  Liegt die Pixelmitte auf der edge0-Kante?
                        //Wenn ja:   IsZero(edge0.Y)  -> Ist es eine Top-Kante
                        //           edge0.X < 0      -> die gegen den Uhrzeigersinn(CCW) verläuft? 
                        //           edge0.Y > 0      -> oder ist es eine Left-Kante? (Läuft im CCW von oben nach unten)
                        //Wenn nein: w0 > 0           -> Liegt die Pixelmitte innerhalb vom Dreieck?
                        overlaps &= (IsZero(w0) ? ((IsZero(edge0.Y) && edge0.X < 0) || edge0.Y > 0) : (w0 > 0));
                        overlaps &= (IsZero(w1) ? ((IsZero(edge1.Y) && edge1.X < 0) || edge1.Y > 0) : (w1 > 0));
                        overlaps &= (IsZero(w2) ? ((IsZero(edge2.Y) && edge2.X < 0) || edge2.Y > 0) : (w2 > 0));
                    }
                    else
                    {
                        overlaps &= (IsZero(w0) ? ((IsZero(edge0.Y) && edge0.X > 0) || edge0.Y < 0) : (w0 < 0));
                        overlaps &= (IsZero(w1) ? ((IsZero(edge1.Y) && edge1.X > 0) || edge1.Y < 0) : (w1 < 0));
                        overlaps &= (IsZero(w2) ? ((IsZero(edge2.Y) && edge2.X > 0) || edge2.Y < 0) : (w2 < 0));
                    }

                    if (overlaps)
                    {
                        //Mit Byzentrische Koordinaten Vertex-Daten interpolieren
                        if (pixelCenterCallBack != null)
                        {
                            w0 /= area;
                            w1 /= area;
                            w2 /= area;
                            pixelCenterCallBack(w0, w1, w2); //Nur die Pixelmitte übergeben
                        }

                        if (pixelEdgesCallBack != null)
                        {
                            pixelEdgesCallBack(new Point(x, y));
                        }                     
                    }
                }
        }

        //Gibt die Distanz des Punktes c zur Kante ab zurück
        private static float EdgeFunction(Vector2D a, Vector2D b, Vector2D c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }

        private static bool IsZero(float f)
        {
            return Math.Abs(f) < 0.0001f;
        }

        //https://math.stackexchange.com/questions/1324179/how-to-tell-if-3-connected-points-are-connected-clockwise-or-counter-clockwise
        private static bool IsCounterClockwise(Vector2D p0, Vector2D p1, Vector2D p2)
        {
            float A = p1.X * p0.Y + p2.X * p1.Y + p0.X * p2.Y;
            float B = p0.X * p1.Y + p1.X * p2.Y + p2.X * p0.Y;
            return A > B;
        }

        private static Rectangle GetBoundingRectangle(Vector2D p0, Vector2D p1, Vector2D p2)
        {
            Vector2D min = new Vector2D(
                Math.Min(Math.Min(p0.X, p1.X), p2.X),
                Math.Min(Math.Min(p0.Y, p1.Y), p2.Y)
                );

            Vector2D max = new Vector2D(
                Math.Max(Math.Max(p0.X, p1.X), p2.X),
                Math.Max(Math.Max(p0.Y, p1.Y), p2.Y)
                );

            int startX = (int)min.X;
            int startY = (int)min.Y;
            int endX = (int)max.X + 1;
            int endY = (int)max.Y + 1;

            return new Rectangle(startX, startY, endX - startX, endY - startY);
        }
    }
}

using System;
using GraphicMinimal;
using System.Drawing;

namespace GraphicPipelineCPU.DrawingHelper.Helper3D
{
    //Diese Klasse stehlt ein einzelnes Pixel im Texturespace dar. An jeden Eckpunkt stehen Texturkoordinaten 
    class Footprint
    {
        public Point WindowPixelPosition { get; private set; } //Innerhalb von diesen Pixel liegt das Footprint-Zentrum
        public Vector2D lo {get; private set;  } // Linke obere Pixelecke
        public Vector2D ro {get; private set;  } // Rechte obere Pixelecke
        public Vector2D lu {get; private set;  } // Linke untere Pixelecke
        public Vector2D ru { get; private set; } // Rechte untere Pixelecke

        private RectangleF? boundingRectangle = null;
        private Vector2D centerPoint = null;

        private Vector2D edge1 = null; // Richtungsvektor lu -> ru
        private Vector2D edge2 = null; // Richtungsvektor lu -> lo

        public RectangleF BoundingRectangle
        {
            get
            {
                if (this.boundingRectangle == null)
                {
                    float minX = Math.Min(Math.Min(this.lo.X, this.ro.X), Math.Min(this.lu.X, this.ru.X));
                    float minY = Math.Min(Math.Min(this.lo.Y, this.ro.Y), Math.Min(this.lu.Y, this.ru.Y));
                    float maxX = Math.Max(Math.Max(this.lo.X, this.ro.X), Math.Max(this.lu.X, this.ru.X));
                    float maxY = Math.Max(Math.Max(this.lo.Y, this.ro.Y), Math.Max(this.lu.Y, this.ru.Y));
                    boundingRectangle = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                }

                return (RectangleF)this.boundingRectangle;
            }
        }

        public Vector2D CenterPoint
        {
            get
            {
                if (this.centerPoint == null)
                {
                    Vector2D loroM = (this.ro - this.lo) / 2 + this.lo;
                    Vector2D luruM = (this.ru - this.lu) / 2 + this.lu;

                    this.centerPoint = (loroM - luruM) / 2 + luruM;
                }

                return this.centerPoint;
            }
        }

        public Footprint(Point windowPixelPosition, Vector2D lo, Vector2D ro, Vector2D lu, Vector2D ru)
        {
            this.WindowPixelPosition = windowPixelPosition;
            this.lo = lo;
            this.ro = ro;
            this.lu = lu;
            this.ru = ru;

            this.edge1 = this.ru - this.lu;
            this.edge2 = this.lo - this.lu;
        }

        public bool IsPointInside(float x, float y)
        {
            Vector2D p = new Vector2D(x, y);
            if (EdgeFunction(lo, lu, p) < 0) return false;
            if (EdgeFunction(lu, ru, p) < 0) return false;
            if (EdgeFunction(ru, ro, p) < 0) return false;
            if (EdgeFunction(ro, lo, p) < 0) return false;

            return true;
        }

        //Gibt die Distanz des Punktes c zur Kante ab zurück
        private static float EdgeFunction(Vector2D a, Vector2D b, Vector2D c)
        {
            return (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
        }

        public Vector2D GetRandomPoint(Random rand)
        {
            float f1 = (float)rand.NextDouble();
            float f2 = (float)rand.NextDouble();

            return this.lu + this.edge1 * f1 + this.edge2 * f2;
        }
    }
}

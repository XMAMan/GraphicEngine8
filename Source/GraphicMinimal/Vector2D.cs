using System;

namespace GraphicMinimal
{
    [Serializable]
    public class Vector2D : IPoint2D
    {
        public float X { get; set; }
        public float Y { get; set; }

        public int Xi
        {
            get
            {
                return (int)X;
            }
        }

        public int Yi
        {
            get
            {
                return (int)Y;
            }
        }

        public Vector2D()
        {

        }

        public Vector2D(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public Vector2D(Vector2D source)
        {
            this.X = source.X;
            this.Y = source.Y;
        }

        public override string ToString()
        {
            return "[" + X.ToString() + ";" + Y.ToString() + "]";
        }

        public static Vector2D Parse(string value)
        {
            var splits = value.Substring(1, value.Length - 2).Split(';');

            return new Vector2D(float.Parse(splits[0]),
                              float.Parse(splits[1]));
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Vector2D p = obj as Vector2D;
            if ((System.Object)p == null)
            {
                return false;
            }

            return X == p.X && Y == p.Y;
        }

        public override int GetHashCode()
        {
            return (int)X ^ (int)Y;
        }

        public float Length()//Länge eines Vektors
        {
            return (float)Math.Sqrt(X * X + Y * Y);
        }

        public float SquareLength() //Länge des Vektors ins Quadrat
        {
            return X * X + Y * Y;
        }

        public Vector2D Normalize()
        {
            return this / Length();
        }

        public Vector2D Abs()
        {
            return new Vector2D(Math.Abs(this.X), Math.Abs(this.Y));
        }

        public Vector2D SignZeroIsOne()
        {
            return new Vector2D(SignZeroIsOne(this.X), SignZeroIsOne(this.Y));
        }

        private static float SignZeroIsOne(float x)
        {
            return x < 0.0f ? -1.0f : 1.0f;
        }

        public Vector2D SignZeroIsZero()
        {
            return new Vector2D(SignZeroIsZero(this.X), SignZeroIsZero(this.Y));
        }

        private static float SignZeroIsZero(float x)
        {
            if (x == 0) return 0;
            return x < 0.0f ? -1.0f : 1.0f;
        }

        public static Vector2D operator +(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
        }
        public static Vector2D operator -(Vector2D v)
        {
            return -1 * v;
        }

        public static float operator *(Vector2D v1, Vector2D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }
        public static Vector2D operator *(Vector2D v, float f)
        {
            return new Vector2D(v.X * f, v.Y * f);
        }
        public static Vector2D operator *(float f, Vector2D v)
        {
            return new Vector2D(v.X * f, v.Y * f);
        }

        public static Vector2D operator /(Vector2D v, float f)
        {
            return new Vector2D(v.X / f, v.Y / f);
        }

        //Return: Wert zwischen 0 und 180
        public static float Angle(Vector2D v1, Vector2D v2)
        {
            float f = (float)(Math.Acos((v1 * v2) / (v1.Length() * v2.Length())) * 180 / Math.PI);
            if (Single.IsNaN(f)) f = 0;
            return f;
        }

        //v1 und v2 sind Richtungsvektoren. v1 wird gedanklich auf Einheitskreis-Startvektor (1,0) gelegt. 
        //v2 wird auch in den Kreis gelegt. Wenn v2.y > 0, dann liegt Winkel zwischen 0 und 180 Grad, sonst zwischen 180 und 360
        public static float Angle360(Vector2D v1, Vector2D v2) //Winkel zwischen zwei Richtungsvektoren
        {
            v1 = v1.Normalize();
            v2 = v2.Normalize();

            float f1 = v1 * v2;
            if (f1 > 1) f1 = 1;
            if (f1 < -1) f1 = -1;
            float f = (float)(Math.Acos(f1) * 180 / Math.PI);

            Vector2D vD = v1.Spin90();
            //Hier steht noch der lange Weg mit Matrix, um das ganze besser zu verstehen. v2Y entspricht v2k.Y
            /*float[] circleMatrix = new float[]{v1.X, vD.X,
                                              v1.Y, vD.Y};

            Vector2D v2K = new Vector2D(circleMatrix[0] * v2.X + circleMatrix[2] * v2.Y,
                                        circleMatrix[1] * v2.X + circleMatrix[3] * v2.Y);

            Vector2D v1K = new Vector2D(circleMatrix[0] * v1.X + circleMatrix[2] * v1.Y, //v1 in Kreis projektziert ergibt immer (1,0)
                                        circleMatrix[1] * v1.X + circleMatrix[3] * v1.Y);

            Vector2D vdK = new Vector2D(circleMatrix[0] * vD.X + circleMatrix[2] * vD.Y, //vD in Kreis projektziert ergibt immer (0,1)
                                        circleMatrix[1] * vD.X + circleMatrix[3] * vD.Y);

            if (v2K.Y < 0) f = 360 - f;*/

            float v2Y = vD.X * v2.X + vD.Y * v2.Y; //Das ist die Y-Koordinate, von vD, welche in den Einheitskreis projektziert wurde
            if (v2Y < 0) f = 360 - f;
            return f;
        }

        public static Vector2D DirectionFromPhi(double phi)
        {
            return new Vector2D((float)Math.Cos(phi), (float)Math.Sin(phi));
        }

        //Diese Funktion ist das Gegenstück zu 'Angle360'. v1 wird um den Winkel 'angle360' gedreht, um v2 zu erhalten. Wenn angle360==0 ist, dann ist der Returnwert = v1
        public static Vector2D GetV2FromAngle360(Vector2D v1, float angle360)
        {
            float v1Length = v1.Length();
            v1 /= v1Length;
            Vector2D vD = v1.Spin90();

            float w = angle360 * (float)Math.PI / 180;
            Vector2D v2 = new Vector2D((float)Math.Cos(w), (float)Math.Sin(w));

            //Das ist die inverse/transpornierte Matrix von oben, um v2 aus dem Kreis-Koordinaten in Weltkoordinaten zu konvertieren
            float[] circleMatrix = new float[]{v1.X, v1.Y,
                                               vD.X, vD.Y};
            Vector2D v2W = new Vector2D(circleMatrix[0] * v2.X + circleMatrix[2] * v2.Y,
                                        circleMatrix[1] * v2.X + circleMatrix[3] * v2.Y);

            return v2W * v1Length;
        }

        //Berechnet sowas ähnliches wie Angle360(new Vector2D(1,0), p2-p1) nur dass es schneller geht und man diesen "Winkelersatz" nicht in 
        //Grad/Rad ausdrücken kann sondern. D.h. wenn ich an p1 stehe und nun wissen will, um wie viel Grad ich mich nach links/rechts drehen muss, um dann
        //in Richtung p2 zu laufen, dann ist diese Funktion hier nicht hilfreich. Stattdessen ist diese Funktion für den Fall, dass ich ein Punkt p1
        //gegeben habe und nun ganz viele verschiedene p2-Punkte. Ich kann nun für jeden p2-Punkt die GetAngle360ForSorting-Richtung bestimmen
        //und dann die p2-Punkte nach diesen Pseude-Winkel sortieren. Das braucht man wenn man eine konvexe Hülle berechnen will.
        //Quelle: Robert Sedgewick - Algorithmen in C++ (Erste Auflage von 1992) - Seite 405 (Dort heißt die Funktion theta)
        public static float GetAngle360ForSorting(Vector2D p1, Vector2D p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float ax = Math.Abs(dx);
            float ay = Math.Abs(dy);
            float s = ax + ay;
            float t = s < 0.0001f ? 0 : dy / s;
            if (dx < 0) t = 2 - t; else if (dy < 0) t = 4 + t;

            return t * 90;
        }

        public static Vector2D RotatePointAroundPivotPoint(Vector2D pivotpoint, Vector2D point, float angleInDegree)
        {
            double angleRadians = angleInDegree * Math.PI / 180;

            Vector2D v = point - pivotpoint;

            return new Vector2D(pivotpoint.X + (float)(Math.Cos(angleRadians) * v.X - Math.Sin(angleRadians) * v.Y),
                                pivotpoint.Y + (float)(Math.Sin(angleRadians) * v.X + Math.Cos(angleRadians) * v.Y));
        }

        public static Vector2D RotatePointAboutYAxis(float xFromYAxis, Vector2D point, float angleInDegree)
        {
            double angleRadians = angleInDegree * Math.PI / 180;
            return new Vector2D(xFromYAxis + (float)(Math.Cos(angleRadians) * (point.X - xFromYAxis)), point.Y);
        }

        //Dreht 90 Grad nach Links (Gegen den Uhrzeigersinn)
        public Vector2D Spin90()
        {
            return new Vector2D(-this.Y, this.X);
        }

        //wenn direction1 um den Return-Wert gedreht wird, dann zeigt es in direction2
        //GetSpinAngle = -180 .. +180 
        //Angle360     = 0 .. 360
        //Wenn Angle360 190 Grad anzeigt, dann gibt GetSpinAngle -170 Grad zurück
        //Wenn Angle360 350 Grad anzeigt, dann gibt GetSpinAngle -10 Grad zuürck
        public static float GetSpinAngle(Vector2D direction1, Vector2D direction2)
        {
            float angle = Vector2D.Angle(direction1, direction2);
            bool isDirection1CounterClockwiseToDirection2 = direction1.X * direction2.Y - direction2.X * direction1.Y >= 0;
            if (isDirection1CounterClockwiseToDirection2 == false)
            {
                angle = -angle;
            }

            return angle;
        }

        public static Vector2D Projection(Vector2D v1, Vector2D v2)//Projektziert v1 senkrecht auf v2
        {
            Vector2D ret = v2 * ((v1 * v2) / (v2 * v2));
            if (!float.IsNaN(ret.X) && !float.IsNaN(ret.Y)) return ret;
            return new Vector2D(0, 0);
        }

        //wenn v länger als maxLength ist, dann wird es auf diese Länge begrenzt. Wenn es kürzer ist, wird nichts dran verändert
        public static Vector2D BoundLength(Vector2D v, float maxLength)
        {
            float length = v.Length();
            if (length < 0.01f) return v;
            float newLength = length;
            if (newLength > maxLength) newLength = maxLength;
            return (v / length) * newLength;
        }

        public static float ZValueFromCross(Vector2D v1, Vector2D v2)
        {
            return v1.X * v2.Y - v2.X * v1.Y;
        }
    }
}

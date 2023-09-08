using GraphicMinimal;
using System;
using System.Drawing;

namespace GraphicGlobal.Rasterizer2DFunctions
{
    public static class CircleArcDrawer
    {
        #region Hier erkläre ich wie ich die DrawCircleArc-Funktion erstellt habe
        //Schritt 1: Verwendung von Cos/Sin für jeden Schritt. Schrittweite von 0.2. Bei großen Kreisen entstehen Löcher und
        //           bei kleinen Kreisen zeichnet man Pixel doppelt
        //           Diese Algorithmus ist inneffektiv aber dafür leicht zu verstehen
        public static void DrawCircleArc1(Vector2D pos, int radius, float startAngle, float endAngle, Action<Vector2D> drawPixel)
        {
            if (startAngle < 0 || startAngle > 360) throw new ArgumentException("startAngle must be in range of 0..360");
            if (endAngle < 0 || endAngle > 360) throw new ArgumentException("endAngle must be in range of 0..360");

            if (endAngle < startAngle) endAngle += 360;
            for (float a = startAngle; a < endAngle; a+=0.2f) //Die Schrittweite von 0.2 ist willkürlich gelegt.
            {
                var p = pos + new Vector2D((float)Math.Cos(a * Math.PI / 180), -(float)Math.Sin(a * Math.PI / 180)) * radius;
                drawPixel(p);
            }
        }

        //Schritt 2: Um ein Kreis so zu zeichnen, dass man wie beim  Bresenham-Line-Algorithmus nur per Integer über 
        //           die Pixel läuft gibt es den Midpoint-Circle-Algorithmus. Siehe: https://en.wikipedia.org/wiki/Midpoint_circle_algorithm
        //           Die C#-Implementierung habe ich von hier: https://rosettacode.org/wiki/Bitmap/Midpoint_circle_algorithm#C#
        //           Der Kreis wird hier in 8 gleichgroße Segmente unterteilt. Um zu sehen wo die Segmente liegen habe
        //           gebe ich sie hier mit unterschiedlichen Farben aus.
        //           Grundidee: x läuft von 0 bis x==y. Das ist das Segment, was von 45..90 Grad geht. 
        //           Legt man im Segment 2 eine Linie rein, dann ist der Y-Anstieg kleiner als 45 Grad. Somit wird
        //           x bei jeden Schritt immer um 1 erhöht. Dagegen wird y nur ab und zu um 1 verringert.    
        public static void DrawCircleArc2(Vector2D pos, int radius, Action<Vector2D, Color> drawPixel)
        {
            int px = (int)pos.X;
            int py = (int)pos.Y;
            int d = (5 - radius * 4) / 4;
            int x = 0;
            int y = radius;

            do
            {
                drawPixel(new Vector2D(px + y, py - x), Color.Blue);   //1 0..45 CCW
                drawPixel(new Vector2D(px + x, py - y), Color.Yellow); //2 90..45 CW
                drawPixel(new Vector2D(px - x, py - y), Color.Orange); //3 90..135 CCW
                drawPixel(new Vector2D(px - y, py - x), Color.Gray);   //4 180..135 CW
                drawPixel(new Vector2D(px - y, py + x), Color.Black);  //5 180..225 CCW
                drawPixel(new Vector2D(px - x, py + y), Color.Magenta);//6 270..225 CW
                drawPixel(new Vector2D(px + x, py + y), Color.Red);    //7 270..315 CCW
                drawPixel(new Vector2D(px + y, py + x), Color.Green);  //8 360..315 CW

                if (d < 0)
                {
                    d += 2 * x + 1;
                }
                else
                {
                    d += 2 * (x - y) + 1;
                    y--;
                }
                x++;
            } while (x <= y);
        }

        //Wenn ich nicht das komplette Segment 2 zeichnen will sondern nur den Bereich von 55..85 dann berechne ich 
        //zuerst von 55 und 85 Grad den Cosinus. x darf dann nur innerhalb der Cos(55) .. Cos(85)-Werte laufen.
        //Wenn ich für ein Segment diese Schranke will, wo der Y-Anstieg größer als 45 Grad ist, dann läuft die 
        //Laufvarialbe x entlang der Y-Achse und X wird nur ab und zu verändert. Ich muss die Schranke dann entlang
        //der Y-Achse aufstellen und dann mit den Sinus-Werten der startAngle/endAngle-Werte arbeiten.
        //In dieser Funktion hier zeichne ich für jedes segment immer nur den 5..35-Grad-Bereich.
        //Auf diese Weise habe ich durch Probieren ermittelt, ob ich jeweils mit der X- oder Y-Achse arbeiten muss,
        //welches Vorzeichen ich für den Schrankenvergleich brauche und ob ich den größer- oder kleiner-Operator brauche.
        public static void DrawCircleArc3(Vector2D pos, int radius, Action<Vector2D, Color> drawPixel)
        {
            for (int segment=0; segment < 8; segment++)
            {
                float startAngle = 5 + 45 * segment;
                float endAngle = 25 + 45 * segment;

                int startX = (int)(Math.Cos(startAngle * Math.PI / 180) * radius);
                int endX = (int)(Math.Cos(endAngle * Math.PI / 180) * radius);
                int startY = (int)(Math.Sin(startAngle * Math.PI / 180) * radius);
                int endY = (int)(Math.Sin(endAngle * Math.PI / 180) * radius);

                int px = (int)pos.X;
                int py = (int)pos.Y;
                int d = (5 - radius * 4) / 4;
                int x = 0;
                int y = radius;

                do
                {
                    if (segment == 0 && x > startY && x < endY) drawPixel(new Vector2D(px + y, py - x), Color.Blue);     //1 0..45 CCW
                    if (segment == 1 && x > endX && x < startX) drawPixel(new Vector2D(px + x, py - y), Color.Yellow);   //2 90..45 CW
                    if (segment == 2 && x > -startX && x < -endX) drawPixel(new Vector2D(px - x, py - y), Color.Orange); //3 90..135 CCW
                    if (segment == 3 && x > endY && x < startY) drawPixel(new Vector2D(px - y, py - x), Color.Gray);    //4 180..135 CW
                    if (segment == 4 && x > -startY && x < -endY) drawPixel(new Vector2D(px - y, py + x), Color.Black);  //5 180..225 CCW
                    if (segment == 5 && x > -endX && x < -startX) drawPixel(new Vector2D(px - x, py + y), Color.Magenta);//6 270..225 CW
                    if (segment == 6 && x > startX && x < endX) drawPixel(new Vector2D(px + x, py + y), Color.Red);      //7 270..315 CCW
                    if (segment == 7 && x > -endY && x < -startY) drawPixel(new Vector2D(px + y, py + x), Color.Green);  //8 360..315 CW


                    if (d < 0)
                    {
                        d += 2 * x + 1;
                    }
                    else
                    {
                        d += 2 * (x - y) + 1;
                        y--;
                    }
                    x++;
                } while (x <= y);
            }            
        }

        //In dieser Funktion werden nur startAngle/endAngle beachtet, wo endAngle größer startAngle ist
        //Wenn ich ein Segment zeichnen will, dann gibt es folgende Möglichkeiten:
        //1 Das Segment liegt vollkommen innerhalb der startAngle/endAngle-Grenze -> Zeichne das Segment vollständig
        //2 Das Segment liegt vollkommen außerhalb der startAngle/endAngle-Grenze -> Zeichne das Segment überhaupt nicht
        //3 Nur endAngle liegt innerhalb des Segments -> Zeichne das Segment vom Start-Bereich bis endAngle
        //4 Nur startAngle liegt innerhalb des Segments -> Zeichne das Segment von startAngle bis End-Bereich
        //5 startAngle und endAngle liegt innerhalb des Segments -> Zeichne das Segment nur von startAngle bis endAngle
        //Für jedes Segment entscheide ich, zu welchen der 5 Fälle es gehört.
        //Der canDraw-Functor gibt für jedes Segment an, ob für das angegebene x/y gezeichnet werden darf oder nicht.
        public static void DrawCircleArc4(Vector2D pos, int radius, float startAngle, float endAngle, Action<Vector2D> drawPixel)
        {
            if (startAngle < 0 || startAngle > 360) throw new ArgumentException("startAngle must be in range of 0..360");
            if (endAngle < 0 || endAngle > 360) throw new ArgumentException("endAngle must be in range of 0..360");

            int px = (int)pos.X;
            int py = (int)pos.Y;
            int d = (5 - radius * 4) / 4;
            int x = 0;
            int y = radius;

            int startX = (int)(Math.Cos(startAngle * Math.PI / 180) * radius);
            int endX = (int)(Math.Cos(endAngle * Math.PI / 180) * radius);
            int startY = (int)(Math.Sin(startAngle * Math.PI / 180) * radius);
            int endY = (int)(Math.Sin(endAngle * Math.PI / 180) * radius);

            int startI = (int)(startAngle / 45); //In diesen Segment-Index liegt startAngle
            int endI = (int)(endAngle / 45);     //In diesen Segment-Index liegt endAngle

            Func<int, bool>[] startCondition = new Func<int, bool>[8];
            startCondition[0] = (u) => u > startY;
            startCondition[1] = (u) => u < startX;
            startCondition[2] = (u) => u > -startX;
            startCondition[3] = (u) => u < startY;
            startCondition[4] = (u) => u > -startY;
            startCondition[5] = (u) => u < -startX;
            startCondition[6] = (u) => u > startX;
            startCondition[7] = (u) => u < -startY;

            Func<int, bool>[] endCondition = new Func<int, bool>[8];
            endCondition[0] = (u) => u < endY;
            endCondition[1] = (u) => u > endX;
            endCondition[2] = (u) => u < -endX;
            endCondition[3] = (u) => u > endY;
            endCondition[4] = (u) => u < -endY;
            endCondition[5] = (u) => u > -endX;
            endCondition[6] = (u) => u < endX;
            endCondition[7] = (u) => u > -endY;

            Func<int, bool>[] canDraw = new Func<int, bool>[8];
            for (int i = 0; i < canDraw.Length; i++)
            {
                if (i < startI || i > endI)
                    canDraw[i] = (u) => false;      //Fall 1: Das Segment liegt außerhalb der startAngle/endAngle-Schranke
                else if (i > startI && i < endI)
                    canDraw[i] = (u) => true;       //Fall 2: Das Segment liegt vollständig innerhalb der startAngle/endAngle-Schranke
                else if (i == startI && i < endI)
                    canDraw[i] = startCondition[i]; //Fall 3: Nur startAngle liegt innerhalb vom Segment
                else if (i > startI && i == endI)
                    canDraw[i] = endCondition[i];   //Fall 4: Nur endAngle liegt innerhalb vom Segment
                else
                {
                    //Fall 5: Sowohl startAngle als auch endAngle liegt innerhalb vom Segment
                    int copy = i; //Diese Variable ist nötig, da der Lambda-Ausdruck nicht den Wert von i hier speichert 
                    //sondern die Adresse auf i. D.h. im letzten Schleifendurchlauf ist i dann 8 und ich bekomme eine
                    //OutOfRange-Exception, wenn ich i anstatt copy im Lambda-Ausdruck verwende
                    canDraw[i] = (u) => startCondition[copy](u) && endCondition[copy](u);
                }
            }

            do
            {
                if (canDraw[0](x)) drawPixel(new Vector2D(px + y, py - x));   //1 0  ..45  CCW
                if (canDraw[1](x)) drawPixel(new Vector2D(px + x, py - y));   //2 90 ..45  CW
                if (canDraw[2](x)) drawPixel(new Vector2D(px - x, py - y));   //3 90 ..135 CCW
                if (canDraw[3](x)) drawPixel(new Vector2D(px - y, py - x));   //4 180..135 CW
                if (canDraw[4](x)) drawPixel(new Vector2D(px - y, py + x));   //5 180..225 CCW
                if (canDraw[5](x)) drawPixel(new Vector2D(px - x, py + y));   //6 270..225 CW
                if (canDraw[6](x)) drawPixel(new Vector2D(px + x, py + y));   //7 270..315 CCW
                if (canDraw[7](x)) drawPixel(new Vector2D(px + y, py + x));   //8 360..315 CW

                if (d < 0)
                {
                    d += 2 * x + 1;
                }
                else
                {
                    d += 2 * (x - y) + 1;
                    y--;
                }
                x++;
            } while (x <= y);
        }
        #endregion

        //Startpunkt für diese Funktion habe ich von hier: https://rosettacode.org/wiki/Bitmap/Midpoint_circle_algorithm#C#
        //Ich habe es dann so angepasst, das einzelne Kreissegmente gezeichnet werden können
        public static void DrawCircleArc(Vector2D center, int radius, float startAngle, float endAngle, bool withBorderLines, Action<Vector2D> pixelCallback)
        {
            //if (startAngle < 0 || startAngle > 360) throw new ArgumentException("startAngle must be in range of 0..360");
            //if (endAngle < 0 || endAngle > 360) throw new ArgumentException("endAngle must be in range of 0..360");

            if (startAngle > 360 || startAngle < 0) startAngle -= (int)(startAngle / 360) * 360;
            if (endAngle > 360 || endAngle < 0) endAngle -= (int)(endAngle / 360) * 360;

            if (startAngle < 0) startAngle += 360;
            if (endAngle < 0) endAngle += 360;

            //Wenn startAngle größer als endAngle ist, dann ist dass das gleiche, als wenn ich so tue, als ob ich von
            //endAngle nach startAngle laufe und dabei aber mit negierter canDraw-Funktion arbeite
            bool swap = startAngle > endAngle;
            if (swap)
            {
                float tmp = startAngle;
                startAngle = endAngle;
                endAngle = tmp;
            }

            int px = (int)center.X;
            int py = (int)center.Y;
            int d = (5 - radius * 4) / 4;
            int x = 0;
            int y = radius;

            int startX = (int)(Math.Cos(startAngle * Math.PI / 180) * radius);
            int endX = (int)(Math.Cos(endAngle * Math.PI / 180) * radius);
            int startY = (int)(Math.Sin(startAngle * Math.PI / 180) * radius);
            int endY = (int)(Math.Sin(endAngle * Math.PI / 180) * radius);

            int startI = (int)(startAngle / 45); //In diesen Segment-Index liegt startAngle
            int endI = (int)(endAngle / 45);     //In diesen Segment-Index liegt endAngle

            Func<int, bool>[] startCondition = new Func<int, bool>[8];
            startCondition[0] = (u) => u > startY;
            startCondition[1] = (u) => u < startX;
            startCondition[2] = (u) => u > -startX;
            startCondition[3] = (u) => u < startY;
            startCondition[4] = (u) => u > -startY;
            startCondition[5] = (u) => u < -startX;
            startCondition[6] = (u) => u > startX;
            startCondition[7] = (u) => u < -startY;

            Func<int, bool>[] endCondition = new Func<int, bool>[8];
            endCondition[0] = (u) => u < endY;
            endCondition[1] = (u) => u > endX;
            endCondition[2] = (u) => u < -endX;
            endCondition[3] = (u) => u > endY;
            endCondition[4] = (u) => u < -endY;
            endCondition[5] = (u) => u > -endX;
            endCondition[6] = (u) => u < endX;
            endCondition[7] = (u) => u > -endY;

            Func<int, bool>[] canDraw = new Func<int, bool>[8];
            for (int i = 0; i < canDraw.Length; i++)
            {
                Func<int, bool> canDraw1;

                if (i < startI || i > endI)
                    canDraw1 = (u) => false;        //Fall 1: Das Segment liegt außerhalb der startAngle/endAngle-Schranke
                else if (i > startI && i < endI)
                    canDraw1 = (u) => true;         //Fall 2: Das Segment liegt vollständig innerhalb der startAngle/endAngle-Schranke
                else if (i == startI && i < endI)
                    canDraw1 = startCondition[i];   //Fall 3: Nur startAngle liegt innerhalb vom Segment
                else if (i > startI && i == endI)   
                    canDraw1 = endCondition[i];     //Fall 4: Nur endAngle liegt innerhalb vom Segment
                else
                {
                    //Fall 5: Sowohl startAngle als auch endAngle liegt innerhalb vom Segment
                    int copy = i; //Diese Variable ist nötig, da der Lambda-Ausdruck nicht den Wert von i hier speichert 
                    //sondern die Adresse auf i. D.h. im letzten Schleifendurchlauf ist i dann 8 und ich bekomme eine
                    //OutOfRange-Exception, wenn ich i anstatt copy im Lambda-Ausdruck verwende
                    canDraw1 = (u) => startCondition[copy](u) && endCondition[copy](u);
                }

                if (swap)
                {
                    canDraw[i] = (u) => !canDraw1(u);
                }else
                {
                    canDraw[i] = canDraw1;
                }
            }

            do
            {
                if (canDraw[0](x)) pixelCallback(new Vector2D(px + y, py - x));   //1 0  ..45  CCW
                if (canDraw[1](x)) pixelCallback(new Vector2D(px + x, py - y));   //2 90 ..45  CW
                if (canDraw[2](x)) pixelCallback(new Vector2D(px - x, py - y));   //3 90 ..135 CCW
                if (canDraw[3](x)) pixelCallback(new Vector2D(px - y, py - x));   //4 180..135 CW
                if (canDraw[4](x)) pixelCallback(new Vector2D(px - y, py + x));   //5 180..225 CCW
                if (canDraw[5](x)) pixelCallback(new Vector2D(px - x, py + y));   //6 270..225 CW
                if (canDraw[6](x)) pixelCallback(new Vector2D(px + x, py + y));   //7 270..315 CCW
                if (canDraw[7](x)) pixelCallback(new Vector2D(px + y, py + x));   //8 360..315 CW

                if (d < 0)
                {
                    d += 2 * x + 1;
                }
                else
                {
                    d += 2 * (x - y) + 1;
                    y--;
                }
                x++;
            } while (x <= y);

            if (withBorderLines)
            {
                Vector2D pStart = new Vector2D(center.X + (float)Math.Cos(startAngle * Math.PI / 180) * radius, center.Y - (float)Math.Sin(startAngle * Math.PI / 180) * radius);
                Vector2D pEnd = new Vector2D(center.X + (float)Math.Cos(endAngle * Math.PI / 180) * radius, center.Y - (float)Math.Sin(endAngle * Math.PI / 180) * radius);

                ShapeDrawer.DrawLine(new Point((int)center.X, (int)center.Y), new Point((int)pStart.X, (int)pStart.Y), pixelCallback);
                ShapeDrawer.DrawLine(new Point((int)center.X, (int)center.Y), new Point((int)pEnd.X, (int)pEnd.Y), pixelCallback);

            }
        }

        //Idee 1: Per Scanline. Problem hier: Wenn zwei Randlinien so dicht zusammen sind, dass kein innerer Bereich dazwischen passt, dann kommt die Scanline durcheinander
        public static void DrawFillCircleArc1(Vector2D center, int radius, float startAngle, float endAngle, Action<Point> pixelCallback)
        {
            byte[,] pix = new byte[radius * 2 + 1, radius * 2 + 1];
            int[] minY = new int[radius * 2 + 1];
            int[] maxY = new int[radius * 2 + 1];

            for (int x=0;x<pix.GetLength(0);x++)
            {
                minY[x] = int.MaxValue;
                maxY[x] = int.MinValue;

                for (int y = 0; y < pix.GetLength(1); y++)
                {
                    pix[x, y] = 0;
                }
            }
                

            int left = (int)center.X - radius;
            int top = (int)center.Y - radius;

            Action<Vector2D> setPixMap = (p) =>
            {
                int xi = (int)p.X - left;
                int yi = (int)p.Y - top;
                pix[xi, yi] = 1;

                if (yi < minY[xi]) minY[xi] = yi;
                if (yi > maxY[xi]) maxY[xi] = yi;
            };

            DrawCircleArc(center, radius, startAngle, endAngle, true, setPixMap);

            for (int x = 0; x < pix.GetLength(0); x++)
            {
                //int state = 0; //0=Outside; 1=Border after Outside; 2=Inside; 3=Border after Inside
                int isInside = 0; //0=Outside; > 0=Inside
                bool isOnBorder = false;
                for (int y = minY[x]; y <= maxY[x]; y++)
                {
                    if (pix[x, y] == 1 && isOnBorder == false)
                    {
                        isOnBorder = true;

                        if (isInside == 0) isInside = 1; //Enter first Border
                        if (isInside == 2) isInside = 3; //Enter second Border
                    }
                    else if (pix[x, y] == 0 && isOnBorder)
                    {
                        isOnBorder = false;

                        if (isInside == 1) isInside = 2; //Leave first Border
                        if (isInside == 3) isInside = 0; //Leave second Border
                    }

                    if (isInside > 0)
                    {
                        pixelCallback(new Point(left + x, top + y));
                    }
                }
            }                          
        }

        //Idee: Zeichne für jeden CircleArc-Randpixel eine Linie zum Kreiszentrum
        public static void DrawFillCircleArc(Vector2D center, int radius, float startAngle, float endAngle, Action<Point> pixelCallback)
        {
            Action<Vector2D> setPixMap = (p) =>
            {
                ShapeDrawer.DrawLine(new Point((int)center.X, (int)center.Y), new Point((int)p.X, (int)p.Y), (v)=> pixelCallback(new Point((int)v.X, (int)v.Y)));
            };

            DrawCircleArc(center, radius, startAngle, endAngle, true, setPixMap);
        }
    }
}

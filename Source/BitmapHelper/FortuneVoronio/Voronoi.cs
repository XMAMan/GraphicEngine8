using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using GraphicMinimal;

namespace BitmapHelper.FortuneVoronio
{
    internal class Voronoi
    {
        public static List<Vertex2D[]> GetVoronoiPolygons(Size imageSize, List<Point> cellPoints)
        {
            List<Point[]> cells = getVoronoiCells(cellPoints, imageSize.Width - 1, imageSize.Height - 1);
            return cells.Select(x => x.Select(y => new Vertex2D(y.X, y.Y, y.X / (float)imageSize.Width, y.Y / (float)imageSize.Height)).ToArray()).ToList();
        }

        private static List<Point[]> getVoronoiCells(List<Point> cellPoints, int Width, int Height)
            {
                Dictionary<Vector, List<Vector[]>> voronoiCells = new Dictionary<Vector, List<Vector[]>>();

                List<Vector> punkte = new List<Vector>();
                
                foreach (var cellPoint in cellPoints)
                {
                    punkte.Add(new Vector(cellPoint.X, cellPoint.Y));
                    voronoiCells.Add(punkte.Last(), new List<Vector[]>());
                }

                VoronoiGraph graph = Fortune.ComputeVoronoiGraph(punkte);

                List<float> oben = new List<float>();
                List<float> unten = new List<float>();
                List<float> rechts = new List<float>();
                List<float> links = new List<float>();
                foreach (VoronoiEdge edge in graph.Edges)
                {
                    float x1, y1, x2, y2;
                    if (edge.IsPartlyInfinite)
                    {
                        x1 = (float)edge.FixedPoint[0];
                        y1 = (float)edge.FixedPoint[1];
                        PointF P = getBorderPoint(edge.DirectionVector, edge.FixedPoint, Width, Height, oben, unten, links, rechts);
                        x2 = P.X;
                        y2 = P.Y;
                    }
                    else
                    {
                        x1 = (float)edge.VVertexA[0];
                        y1 = (float)edge.VVertexA[1];
                        x2 = (float)edge.VVertexB[0];
                        y2 = (float)edge.VVertexB[1];

                        if ((x1 < 0 || y1 < 0 || x1 > Width || y1 > Height) && (x2 > 0 && y2 > 0 && x2 < Width && y2 < Height))
                        {
                            PointF P = getBorderPoint(new Vector(x1, y1) - new Vector(x2, y2), new Vector(x2, y2), Width, Height, oben, unten, links, rechts);
                            x1 = P.X;
                            y1 = P.Y;
                        }
                        else
                            if ((x2 < 0 || y2 < 0 || x2 > Width || y2 > Height) && (x1 > 0 && y1 > 0 && x1 < Width && y1 < Height))
                            {
                                PointF P = getBorderPoint(new Vector(x2, y2) - new Vector(x1, y1), new Vector(x1, y1), Width, Height, oben, unten, links, rechts);
                                x2 = P.X;
                                y2 = P.Y;
                            }
                    }


                    if (x1 >= 0 && y1 >= 0 && x1 <= Width && y1 <= Height &&
                        x2 >= 0 && y2 >= 0 && x2 <= Width && y2 <= Height)
                    {
                        Vector mx = new Vector((x2 - x1) / 2 + x1, (y2 - y1) / 2 + y1);
                        var liste = (from w in punkte select new { Punkt = w, Abstand = Vector.Dist(mx, w) }).OrderBy(w => w.Abstand).ToList();
                        voronoiCells[liste[0].Punkt].Add(new Vector[] { new Vector(x1, y1), new Vector(x2, y2) });
                        voronoiCells[liste[1].Punkt].Add(new Vector[] { new Vector(x1, y1), new Vector(x2, y2) });
                    }
                    else
                    {
                        int error = 0;
                        string test = error.ToString();
                    }
                }
                oben.Sort();
                unten.Sort();
                links.Sort();
                rechts.Sort();

                Dictionary<Vector, List<Vector[]>> voronoiCellsNew = new Dictionary<Vector, List<Vector[]>>();
                foreach (var cell in voronoiCells.Keys)
                    if (voronoiCells[cell].Count > 0)
                    {
                        //Schritt 1: Sortiere die Zell-Kanten sowohl aufsteigend als auch in ihrer Ausrichtung
                        List<Vector[]> sortedList = new List<Vector[]>();
                        sortedList.Add(voronoiCells[cell][0]);
                        voronoiCells[cell].RemoveAt(0);
                    start1:
                        for (int i = 0; i < voronoiCells[cell].Count; i++) // Füge dahinter ein
                        {
                            bool b1 = voronoiCells[cell][i][0].CompareTo(sortedList.Last()[1]) == 0;
                            bool b2 = voronoiCells[cell][i][1].CompareTo(sortedList.Last()[1]) == 0;
                            if (b1 || b2)
                            {
                                if (b2)
                                {
                                    Vector tmp = voronoiCells[cell][i][0];
                                    voronoiCells[cell][i][0] = voronoiCells[cell][i][1];
                                    voronoiCells[cell][i][1] = tmp;
                                }
                                sortedList.Add(voronoiCells[cell][i]);
                                voronoiCells[cell].RemoveAt(i);
                                goto start1;
                            }
                        }
                    start2:
                        for (int i = 0; i < voronoiCells[cell].Count; i++) // Füge davor ein
                        {
                            bool b1 = voronoiCells[cell][i][0].CompareTo(sortedList.First()[0]) == 0;
                            bool b2 = voronoiCells[cell][i][1].CompareTo(sortedList.First()[0]) == 0;
                            if (b1 || b2)
                            {
                                if (b1)
                                {
                                    Vector tmp = voronoiCells[cell][i][0];
                                    voronoiCells[cell][i][0] = voronoiCells[cell][i][1];
                                    voronoiCells[cell][i][1] = tmp;
                                }
                                sortedList.Insert(0, voronoiCells[cell][i]);
                                voronoiCells[cell].RemoveAt(i);
                                goto start2;
                            }
                        }

                        //Schritt 2: Stopfe Löcher in der sortierten Liste
                        for (int i = 0; i < sortedList.Count; i++)
                        {
                            if (sortedList[i][1].CompareTo(sortedList[(i + 1) % sortedList.Count][0]) != 0)
                            {
                                sortedList.Insert((i + 1) % sortedList.Count, new Vector[] { sortedList[i][1], sortedList[(i + 1) % sortedList.Count][0] });
                                break;
                            }
                        }

                        voronoiCellsNew.Add(cell, sortedList);
                    }

                voronoiCells = voronoiCellsNew;
                List<Point[]> polygone = new List<Point[]>();
                foreach (var cell in voronoiCells.Keys)
                {
                    polygone.Add((from w in voronoiCells[cell] select new Point((int)w[0][0], (int)w[0][1])).ToArray());
                }

                //Hier ist meiner Meinung ein Fehler: Es fehlt der Fall, wo z.B. oben und unten was ist aber links/rechts nichts. Außerdem: Was macht er, wenn der Counter mehr als 1 ist?
                if (oben.Count > 0 && links.Count > 0) polygone.Add(new Point[] { new Point(0, 0), new Point((int)oben.First(), 0), new Point(0, (int)links.First()) });           //Oben Links
                if (oben.Count > 0 && rechts.Count > 0) polygone.Add(new Point[] { new Point(Width, 0), new Point((int)oben.Last(), 0), new Point(Width, (int)rechts.First()) });   //Oben Rechts
                if (links.Count > 0 && unten.Count > 0) polygone.Add(new Point[] { new Point(0, Height), new Point(0, (int)links.Last()), new Point((int)unten.First(), Height) });       //Unten Links
                if (unten.Count > 0 && rechts.Count > 0) polygone.Add(new Point[] { new Point(Width, Height), new Point((int)unten.Last(), Height), new Point(Width, (int)rechts.Last()) }); //Unten Rechts

                //Von mir: Entferne die Ziepfelmützen für jede Kante (Ich gehe davon aus, dass es bei Kante nur 0 oder 1 Schnittpunkt gibt) Es gibt weiterhin Löcher, wenn es mehr als ein Schnittpunkt mit einer Außenkante gibt
                if (oben.Count > 0 && unten.Count > 0 && links.Count == 0) polygone.Add(new Point[] { new Point((int)oben.First(), 0), new Point((int)unten.First(), Height),  new Point(0, Height), new Point(0, 0) });
                if (oben.Count > 0 && unten.Count > 0 && rechts.Count == 0) polygone.Add(new Point[] { new Point((int)unten.Last(), Height), new Point((int)oben.Last(), 0), new Point(Width, 0), new Point(Width, Height), });
                if (links.Count > 0 && rechts.Count > 0 && oben.Count == 0) polygone.Add(new Point[] { new Point(Width, (int)rechts.First()), new Point(0, (int)links.First()), new Point(0, 0), new Point(Width, 0) });
                if (links.Count > 0 && rechts.Count > 0 && unten.Count == 0) polygone.Add(new Point[] { new Point(0, (int)links.Last()), new Point(Width, (int)rechts.Last()), new Point(Width, Height), new Point(0, Height) });

                return polygone;
            }

            private static PointF getBorderPoint(Vector Direction, Vector FixedPoint, int Width, int Height, List<float> oben, List<float> unten, List<float> links, List<float> rechts)
            {
                float x1 = (float)FixedPoint[0], y1 = (float)FixedPoint[1];
                float x2 = 0, y2 = 0;
                if (Direction[0] >= 0) // x >= 0
                {
                    if (Direction[1] >= 0) // y >= 0
                    {
                        //x >= 0 && y >= 0
                        float yp1 = y1 + (float)(1 / Direction[0] * Direction[1]) * (Width - x1); //Schnittpunkt Rechts (Width; yp1)
                        if (yp1 >= 0 && yp1 <= Height)
                        {
                            x2 = Width;
                            y2 = yp1;
                            rechts.Add(yp1);
                            goto ende;
                        }
                        float xp1 = x1 + (float)(1 / Direction[1] * Direction[0]) * (Height - y1); //Schnittpunkt Unten (xp1, Height)
                        x2 = xp1;
                        y2 = Height;
                        unten.Add(xp1);
                        goto ende;
                    }
                    else
                    {
                        //x >= 0 && y < 0
                        float yp1 = y1 + (float)(1 / Direction[0] * Direction[1]) * (Width - x1); //Schnittpunkt Rechts (Width; yp1)
                        if (yp1 >= 0 && yp1 <= Height)
                        {
                            x2 = Width;
                            y2 = yp1;
                            rechts.Add(yp1);
                            goto ende;
                        }
                        float xp1 = x1 + (float)(1 / -Direction[1] * Direction[0]) * y1; //Schnittpunkt Oben (xp1, 0)
                        x2 = xp1;
                        y2 = 0;
                        oben.Add(xp1);
                        goto ende;
                    }
                }
                else
                {
                    if (Direction[1] >= 0) // y >= 0
                    {
                        //x < 0 && y >= 0
                        float yp1 = y1 + (float)(1 / -Direction[0] * Direction[1]) * x1; //Schnittpunkt Links (0; yp1)
                        if (yp1 >= 0 && yp1 <= Height)
                        {
                            x2 = 0;
                            y2 = yp1;
                            links.Add(yp1);
                            goto ende;
                        }
                        float xp1 = x1 + (float)(1 / Direction[1] * Direction[0]) * (Height - y1); //Schnittpunkt Unten (xp1, Height)
                        x2 = xp1;
                        y2 = Height;
                        unten.Add(xp1);
                        goto ende;
                    }
                    else
                    {
                        //x < 0 && y < 0
                        float yp1 = y1 + (float)(1 / -Direction[0] * Direction[1]) * x1; //Schnittpunkt Links (0; yp1)
                        if (yp1 >= 0 && yp1 <= Height)
                        {
                            x2 = 0;
                            y2 = yp1;
                            links.Add(yp1);
                            goto ende;
                        }
                        float xp1 = x1 + (float)(1 / -Direction[1] * Direction[0]) * y1; //Schnittpunkt Oben (xp1, 0)
                        x2 = xp1;
                        y2 = 0;
                        oben.Add(xp1);
                        goto ende;
                    }
                }
            ende: ;
                return new PointF(x2, y2);
            }
        }
}

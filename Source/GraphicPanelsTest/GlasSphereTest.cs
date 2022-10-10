using GraphicPanels;
using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using System.Collections.Generic;
using BitmapHelper;

namespace GraphicPanelsTest
{
    //Hiermit versuche ich das Verhalten von Glaskugeln zu verstehen und wie der Strahlenverlauf dort ist
    [TestClass]
    public class GlasSphereTest
    {
        private static string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        [TestMethod]
        public void CreateGlasSphereImage()
        {
            List<Bitmap> images = new List<Bitmap>();
            for (float refractionIndex = 1.1f; refractionIndex < 2;refractionIndex +=0.1f)
            {
                images.Add(GetImage(refractionIndex));
            }
            BitmapHelp.TransformBitmapListToCollum(images).Save(WorkingDirectory + "GlasSphere2D.bmp");
        }

        private Bitmap GetImage(float refractionIndex)
        {
            GraphicPanel2D graphic = new GraphicPanel2D() { Width = 400, Height = 100, Mode = Mode2D.CPU };

            Vector3D spherePos = new Vector3D(200, graphic.Height / 2, 0);
            float sphereRadius = graphic.Height / 2 - 10;

            Color[] colorTable = new[]
            {
                Color.Plum,
                Color.Turquoise,
                Color.DeepPink,
                Color.Yellow,
                Color.Blue,
                Color.Black,
                Color.Green,
                Color.Brown,
                Color.DarkBlue,
                Color.DarkGreen
            };

            Vector3D cameraPos = new Vector3D(1, graphic.Height / 2, 0);

            graphic.ClearScreen(Color.White);
            graphic.DrawCircle(Pens.Black, spherePos.XY, sphereRadius);

            int rayCount = 10;
            for (int i = 0; i <= rayCount; i++)
            {
                Vector3D direction = Vector3D.Normalize(new Vector3D(spherePos.X, spherePos.Y - sphereRadius + i / (float)rayCount * 2 * sphereRadius, 0) - cameraPos);
                Ray ray = new Ray(cameraPos, direction);

                //Parallele Strahlen treffen die Kugel
                //ray.Start.Y = spherePos.Y - sphereRadius + i / (float)rayCount * 2 * sphereRadius;
                //ray.Direction.Y = 0;

                Vector3D runningPoint = cameraPos;
                bool isOutside = true;
                float refractionIndex1 = float.NaN;
                float refractionIndex2 = float.NaN;
                int recursionDeep = 5;

                var rayPen = new Pen(colorTable[i % colorTable.Length]);

                while (--recursionDeep > 0)
                {
                    float t = IntersectionHelper.GetIntersectionPointDistanceBetweenRayAndSphere(ray, spherePos, sphereRadius);
                    if (float.IsNaN(t))
                    {
                        graphic.DrawLine(rayPen, ray.Start.XY, (ray.Start + ray.Direction * 100).XY);
                        break;
                    }

                    Vector3D lastPos = new Vector3D(runningPoint);
                    runningPoint = ray.Start + ray.Direction * t;

                    graphic.DrawLine(rayPen, lastPos.XY, runningPoint.XY);

                    Vector3D normal = Vector3D.Normalize(runningPoint - spherePos);
                    if (normal * ray.Direction > 0) normal = -normal;

                    graphic.DrawLine(Pens.Red, runningPoint.XY, (runningPoint + normal * 10).XY);

                    if (isOutside)
                    {
                        refractionIndex1 = 1;
                        refractionIndex2 = refractionIndex;
                    }
                    else
                    {
                        refractionIndex1 = refractionIndex;
                        refractionIndex2 = 1;
                    }

                    bool totalReflection = Vector3D.FresnelTerm(-ray.Direction, normal, refractionIndex1, refractionIndex2) == 1;
                    if (totalReflection)
                        ray = new Ray(runningPoint, Vector3D.GetReflectedDirection(ray.Direction, normal));
                    else
                    {
                        ray = new Ray(runningPoint, Vector3D.GetRefractedDirection(-ray.Direction, normal, refractionIndex1, refractionIndex2));
                        isOutside = !isOutside;
                    }

                }
            }

            graphic.DrawString(new Vector2D(0, 0), Color.Black, 10, $"IOR={refractionIndex.ToString("0.00")}");

            return graphic.GetScreenShoot();
        }
    }
}

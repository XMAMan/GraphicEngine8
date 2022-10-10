using BitmapHelper;
using GraphicGlobal;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace RasterizerTest
{
    [TestClass]
    public class FrustumCullingTest
    {
        private string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;
        private static string DataDirectory = UnitTestHelper.FilePaths.DataDirectory;
        private static string[] textures = new[] { "120klein.jpg", "1920px-Ikea_logo.svg.png", "Decal.bmp", "Fenster3.png" };

        [TestMethod]
        public void CompareFrustumSceneWithNoFrustumScene()
        {
            GraphicPanel3D panel = new GraphicPanel3D() { Width = 200, Height = 200, Mode = Mode3D.Direct3D_11 };
            Random rand = new Random(0);

            List<Bitmap> rows = new List<Bitmap>();

            for (int i=0;i<10;i++)
            {
                CreateRandomSphereScene(panel, rand);

                panel.GlobalSettings.UseFrustumCulling = true;
                var start = DateTime.Now;
                Bitmap withCulling = panel.GetSingleImage(panel.Width, panel.Height);
                BitmapHelp.WriteToBitmap(withCulling, ((int)(DateTime.Now - start).TotalMilliseconds).ToString(), Color.Black);

                panel.GlobalSettings.UseFrustumCulling = false;
                start = DateTime.Now;
                Bitmap withoutCulling = panel.GetSingleImage(panel.Width, panel.Height);
                BitmapHelp.WriteToBitmap(withoutCulling, ((int)(DateTime.Now - start).TotalMilliseconds).ToString(), Color.Black);

                rows.Add(BitmapHelp.TransformBitmapListToRow(new List<Bitmap>() { withCulling, withoutCulling }));

                Assert.IsTrue(UnitTestHelper.BitmapHelper.CompareTwoBitmaps(withCulling, withoutCulling));
            }

            panel.Dispose();

            BitmapHelp.TransformBitmapListToCollum(rows).Save(WorkingDirectory + "FrustumCulling.png", ImageFormat.Png);
        }

        private void CreateRandomSphereScene(GraphicPanel3D panel, Random rand)
        {
            float sceneRadius = 1000;
            float maxSphereRadius = 10;
            int sphereCount = 200;

            panel.RemoveAllObjekts();

            panel.GlobalSettings.BackgroundImage = "#FFFFFF";

            for (int i=0;i<sphereCount;i++)
            {
                float distance = 10 + (float)rand.NextDouble() * sceneRadius;
                Vector3D spherePosition = Vector3D.GetRandomDirection(rand.NextDouble(), rand.NextDouble()) * distance;
                float sphereRadius = 1 + (float)rand.NextDouble() * maxSphereRadius;

                string texture = textures[rand.Next(textures.Length)];

                panel.AddSphere(sphereRadius, 5, 5, new ObjectPropertys() { Position = spherePosition, TextureFile = DataDirectory + texture });
            }

            Frame cameraDirection = new Frame(Vector3D.GetRandomDirection(rand.NextDouble(), rand.NextDouble()));
            panel.GlobalSettings.Camera = new Camera()
            {
                Position = new Vector3D(0, 0, 0),
                Forward = cameraDirection.Normal,
                Up = cameraDirection.Tangent,
                OpeningAngleY = 30 + (float)rand.NextDouble() * 40
            };
        }
    }
}

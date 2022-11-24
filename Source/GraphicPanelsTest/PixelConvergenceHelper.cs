using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using GraphicPanels;
using GraphicMinimal;

namespace GraphicPanelsTest
{
    //Diese Klasse hilft bei 3 Dingen:
    //-Wie viel Samples brauche ich (z.B. beim Pathtracing) damit der Farbwert für ein Pixel gegen den richtigen Wert konvergiert? -> GetPixelConvergenzImage
    //-Welches Verfahren(Pathtracing/DirectLighting) konvergiert schneller für ein gegebenen Pixel -> StartTest
    //-Was ist besser? Wenn ich Absorbation-Sampeln vor dem Brdf-Richtungssampeln mache oder danch (Danach kann ich die ContinationPdf==BrdfAfterSapmling setzen)
    public class PixelConvergenceHelper
    {

        //-Was ist besser? Wenn ich Absorbation-Sampeln vor dem Brdf-Richtungssampeln mache oder danch (Danach kann ich die ContinationPdf==BrdfAfterSapmling setzen)
        public static void WritePixelDataToFile(GraphicPanel3D panel, string description, Mode3D mode, int pixX, int pixY, int sampleCount, string fileName)
        {
            panel.Mode = mode;
            List<Vector3D> colorValuesRaw = panel.GetNPixelSamples(panel.Width, panel.Height, null, pixX, pixY, sampleCount);
            List<float> colorValues = colorValuesRaw.Select(x => VectorToColor(x)).ToList();

            List<float> realColorValues = new List<float>();
            float sum = 0;
            for (int i = 0; i < colorValues.Count; i++)
            {
                sum += colorValues[i];
                realColorValues.Add(sum / (i + 1));
            }
            var data = new PixelSampleData(pixX, pixY, realColorValues, description);
            PixelSampleData.WriteToFile(data, fileName);
        }

        public static Bitmap GetConvergenceImageFromFiles(int width, int height, string[] pixelDataFiles)
        {
            var dataList = pixelDataFiles.Select(x => PixelSampleData.ReadFromFile(x)).ToList();
            
            return GetImage(width, height, new PixelData(dataList).GetDrawingData());
        }

        //-Wie viel Samples brauche ich (z.B. beim Pathtracing) damit der Farbwert für ein Pixel gegen den richtigen Wert konvergiert?
        public static Bitmap GetPixelConvergenceImage(GraphicPanel3D panel, Mode3D modus, int pixX, int pixY, int sampleCount)
        {
            return GetImage(panel.Width, panel.Height, CreatePixelData(panel, new List<Mode3D>() { modus }, panel.Width, panel.Height, null, pixX, pixY, sampleCount, VectorToColor).GetDrawingData());
        }

        public static Bitmap GetPixelConvergenceImage(GraphicPanel3D panel, int width, int height, Mode3D modus, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount)
        {
            return GetImage(panel.Width, panel.Height, CreatePixelData(panel, new List<Mode3D>() { modus }, width, height, pixelRange, pixX, pixY, sampleCount, VectorToColor).GetDrawingData());
        }

        //-Welches Verfahren(Pathtracing/DirectLighting) konvergiert schneller für ein gegebenen Pixel
        public static Bitmap StartTest(GraphicPanel3D panel, List<Mode3D> modi)
        {
            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, panel.Width / 2, panel.Height - 20, 100000, VectorToColor).GetDrawingData()); //Für Szene -1
            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, 303, 175, 10000, VectorToColor).GetDrawingData()); //Grüne Kugel; Wenn ich anstat Richtungslicht einfach nur Kugellicht nehme, dann stimmt der Pathtracer
            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, 240, 200, 10000, VectorToColor).GetDrawingData()); //Erster Pixel, den der normaler ColorEstimator auswählt
            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, 129, 209, 10000, VectorToColor).GetDrawingData());//Kornellbox-Glaskugel heller Fleck über der Rechteckspiegelung
            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, 254, 123, 10000, VectorToColor).GetDrawingData());//Grüner Ring
            return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, panel.Width, panel.Height, null, 90, 310, 10000, VectorToColor).GetDrawingData());//Gelbe Mariospiegelung

            //return GetImage(panel.Width, panel.Height, CreatePixelData(panel, modi, 61, 227, 10, VectorToColor).GetDrawingData());
        }

        public static string StartMasterTest()
        {
            List<Mode3D> modi = new List<Mode3D>() { Mode3D.PathTracer, Mode3D.BidirectionalPathTracing, Mode3D.Photonmapping };

            List<SceneTestData> tests = new List<SceneTestData>()
            {
                new SceneTestData(TestScenes.AddTestscene1_RingSphere, "RingSphere"){ Pixels = new List<Pixel>(){ new Pixel(129, 228, 102)}}, //Gelb am Boden
                new SceneTestData(TestScenes.AddTestscene1_RingSphere, "RingSphere"){ Pixels = new List<Pixel>(){ new Pixel(254, 123, 143) }}, //Grüner Ring
                new SceneTestData(TestScenes.AddTestscene1_RingSphere, "RingSphere"){ Pixels = new List<Pixel>(){ new Pixel(90, 310, 143) }}, //Gelbe Mariospiegelung
                new SceneTestData(TestScenes.AddTestscene2_NoWindowRoom, "NoWindowRoom"){ Pixels = new List<Pixel>(){ new Pixel(303, 175, 242) }},//Grüne Kugel (Lichtquelle zu klein für Pathtracer?)
                new SceneTestData(TestScenes.AddTestscene2_NoWindowRoom, "NoWindowRoom"){ Pixels = new List<Pixel>(){ new Pixel(178, 169, 208) }},//Schrank
                new SceneTestData(TestScenes.AddTestscene5_Cornellbox, "Cornellbox"){ Pixels = new List<Pixel>(){ new Pixel(343, 113, 164) }},//Grüne Wand
                new SceneTestData(TestScenes.AddTestscene5_Cornellbox, "Cornellbox"){ Pixels = new List<Pixel>(){ new Pixel(53, 177, 43)}},//Großer Würfel vorne
                new SceneTestData(TestScenes.AddTestscene5_Cornellbox, "Cornellbox"){ Pixels = new List<Pixel>(){ new Pixel(129, 209, 158) }},//Glaskugel heller Fleck über der Rechteckspiegelung
            };

            StringBuilder str = new StringBuilder();
            bool allOk = true;
            int errorCounter = 0;
            foreach (var test in tests)
            {
                GraphicPanel3D panel = new GraphicPanel3D() { Width = 420, Height = 328 };
                panel.GlobalSettings.PhotonCount = 100000;
                test.AddSzeneMethod(panel);

                foreach (var pixel in test.Pixels)
                {
                    List<float> resultValues = new List<float>();
                    foreach (var mod in modi)
                    {
                        float color = GetColorValueFromPixel(panel, mod, pixel.X, pixel.Y, mod == Mode3D.PathTracer ? 100000 : 10000);
                        resultValues.Add(color);
                    }

                    float minColor = resultValues.Min();
                    float maxColor = resultValues.Max();
                    float median = minColor + (maxColor - minColor) / 2;

                    for (int i = 0; i < modi.Count; i++)
                    {
                        float error = Math.Abs(resultValues[i] - median);
                        bool testOk = error < 10;
                        if (testOk == false)
                        {
                            allOk = false;
                            errorCounter += (int)error;
                        }

                        string line = test.TestName + " " + modi[i] + " " + pixel + " Color = " + resultValues[i] + "; Expexted Color = " + median;

                        str.Append(line + new string(' ', 100 - line.Length) + (testOk ? "OK" : "FALSE " + (int)error) + System.Environment.NewLine);
                    }
                }

                panel.Dispose();
            }

            str.Append(System.Environment.NewLine + "All ok: " + allOk + System.Environment.NewLine);
            str.Append("ErrorCounter: " + errorCounter);
            return str.ToString();
        }

        class SceneTestData
        {
            public Action<GraphicPanel3D> AddSzeneMethod;
            public List<Pixel> Pixels;
            public string TestName;

            public SceneTestData(Action<GraphicPanel3D> addSzeneMethod, string testName)
            {
                this.AddSzeneMethod = addSzeneMethod;
                this.TestName = testName;
            }
        }

        class Pixel
        {
            public int X;
            public int Y;
            public float ExpectedColor;

            public Pixel(int x, int y, float expectedColor)
            {
                this.X = x;
                this.Y = y;
                this.ExpectedColor = expectedColor;
            }

            public override string ToString()
            {
                return "[" + X + ", " + Y + "]";
            }
        }

        private static float GetColorValueFromPixel(GraphicPanel3D panel, Mode3D modus, int pixX, int pixY, int sampleCount)
        {
            panel.Mode = modus;
            List<Vector3D> colorValuesRaw = panel.GetNPixelSamples(panel.Width, panel.Height, null, pixX, pixY, sampleCount);
            List<float> colorValues = colorValuesRaw.Select(x => VectorToColor(x)).ToList();
            return colorValues.Sum() / colorValues.Count;
        }

        private static float VectorToColor(Vector3D color)
        {
            return color * new Vector3D(0.2126f, 0.7152f, 0.0722f) * 255;
        }

        private static PixelData CreatePixelData(GraphicPanel3D panel, List<Mode3D> modi, int imageWidth, int imageHeight, ImagePixelRange pixelRange, int pixX, int pixY, int sampleCount, Func<Vector3D, float> colorConverter)
        {
            List<PixelSampleData> list = new List<PixelSampleData>();
            foreach (var mod in modi)
            {
                panel.Mode = mod;
                List<Vector3D> colorValuesRaw = panel.GetNPixelSamples(imageWidth, imageHeight, pixelRange, pixX, pixY, sampleCount);

                List<float> colorValues = colorValuesRaw.Select(x => colorConverter(x)).ToList();

                List<float> realColorValues = new List<float>();
                float sum = 0;
                for (int i = 0; i < colorValues.Count; i++)
                {
                    sum += colorValues[i];
                    realColorValues.Add(sum / (i + 1));
                }
                var data = new PixelSampleData(pixX, pixY, realColorValues, mod.ToString());
                list.Add(data);
            }
            return new PixelData(list);
        }

        private static Bitmap GetImage(int width, int height, DrawingData drawingData)
        {
            Bitmap bild = new Bitmap(width, height);
            Graphics grx = Graphics.FromImage(bild);
            grx.Clear(Color.White);
            grx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            grx.TextContrast = 4;

            Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Violet, Color.RosyBrown, Color.DarkBlue, Color.Orange };

            grx.DrawString(drawingData.ModiList[0].PixX + " " + drawingData.ModiList[0].PixY + "[" + (int)drawingData.MinColorValue + ";" + (int)drawingData.MaxColorValue + "]", new Font("Arial", 10), Brushes.Black, 1, 1);

            float h = 0.8f;

            for (int i = 0; i < drawingData.ModiList.Count; i++)
            {
                grx.DrawString(drawingData.ModiList[i].Description, new Font("Arial", 10), new SolidBrush(colors[i % colors.Length]), 1, i * 15 + 20);
                for (int y = 0; y < drawingData.ModiList[i].ColorValues.Count - 1; y++)
                {
                    grx.DrawLine(new Pen(colors[i % colors.Length]), drawingData.ModiList[i].ColorValues[y].X * width, height - (drawingData.ModiList[i].ColorValues[y].Y * (height * h) + height * (1 - h)), drawingData.ModiList[i].ColorValues[y + 1].X * width, height - (drawingData.ModiList[i].ColorValues[y + 1].Y * (height * h) + height * (1 - h)));
                }
            }

            grx.Dispose();

            return bild;
        }
    }

    class PixelData
    {
        private List<PixelSampleData> modiList;

        public PixelData(List<PixelSampleData> modiList)
        {
            this.modiList = modiList;
        }

        public DrawingData GetDrawingData()
        {
            float minY = this.modiList.Min(x => x.MinColorValue);
            float maxY = this.modiList.Max(x => x.MaxColorValue);
            float maxX = this.modiList.Max(x => x.ColorCount);

            float minColor = float.MaxValue;
            float maxColor = float.MinValue;

            List<PixelDrawingData> list = new List<PixelDrawingData>();
            foreach (var mod in this.modiList)
            {
                List<Vector2D> colDraw = new List<Vector2D>();
                for (int x = 0; x < mod.ColorValues.Count; x++)
                {
                    float col = mod.ColorValues[x];
                    if (x == mod.ColorValues.Count - 1)
                    {
                        if (col < minColor) minColor = col;
                        if (col > maxColor) maxColor = col;
                    }
                    colDraw.Add(new Vector2D(x / maxX, (minY == maxY) ? 0 : (col - minY) / (maxY - minY)));
                }
                list.Add(new PixelDrawingData(mod.PixX, mod.PixY, colDraw, mod.Description));
            }

            return new DrawingData(list, minColor, maxColor);
        }
    }

    class DrawingData
    {
        public List<PixelDrawingData> ModiList { get; private set; }
        public float MinColorValue { get; private set; }
        public float MaxColorValue { get; private set; }

        public DrawingData(List<PixelDrawingData> modiList, float minColorValue, float maxColorValue)
        {
            this.ModiList = modiList;
            this.MinColorValue = minColorValue;
            this.MaxColorValue = maxColorValue;
        }
    }

    class PixelDrawingData
    {
        public int PixX { get; private set; }
        public int PixY { get; private set; }
        public List<Vector2D> ColorValues { get; private set; }//Alle Werte sind auf 0..1 normiert
        public string Description { get; private set; }

        public PixelDrawingData(int pixX, int pixY, List<Vector2D> colorValues, string description)
        {
            this.PixX = pixX;
            this.PixY = pixY;
            this.ColorValues = colorValues;
            this.Description = description;
        }
    }

    [Serializable]
    class PixelSampleData
    {
        public static void WriteToFile(PixelSampleData data, string fileName)
        {
            var fileStream = File.Create(fileName);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fileStream, data);
            fileStream.Close();
        }

        public static PixelSampleData ReadFromFile(string fileName)
        {
            var fileStream = File.Open(fileName, FileMode.Open);
            BinaryFormatter b = new BinaryFormatter();
            var data = (PixelSampleData)b.Deserialize(fileStream);
            fileStream.Close();

            return data;
        }

        public int PixX { get; private set; }
        public int PixY { get; private set; }
        public List<float> ColorValues { get; private set; }
        public string Description { get; private set; }

        public PixelSampleData(int pixX, int pixY, List<float> colorValues, string description)
        {
            this.PixX = pixX;
            this.PixY = pixY;
            this.ColorValues = colorValues;
            this.Description = description;
        }

        public float MinColorValue
        {
            get
            {
                return this.ColorValues.Min();
            }
        }

        public float MaxColorValue
        {
            get
            {
                return this.ColorValues.Max();
            }
        }

        public int ColorCount
        {
            get
            {
                return this.ColorValues.Count;
            }
        }
    }
}

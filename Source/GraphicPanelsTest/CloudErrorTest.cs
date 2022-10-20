using BitmapHelper;
using GraphicMinimal;
using GraphicPanels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphicPanelsTest
{
    //Untersucht das Wolkenkantenproblem
    [TestClass]
    public class CloudErrorTest
    {
        private readonly string WorkingDirectory = UnitTestHelper.FilePaths.WorkingDirectory;

        //Farbverlauf von 9 Pixeln an der rechten Wolke (Siehe CloudErrorTest.CreateSingleLineFromCloud)
        [TestMethod]
        [Ignore] //Hiermit kann ich den Fehler nachstellen -> Siehe Documentation\Clouds-Error für meine Überlegungen dazu
        public void CreateSingleLineFromCloud()
        {
            var line1 = GetPixelLine(Mode3D.ThinMediaSingleScatteringBiased);
            var line2 = GetPixelLine(Mode3D.ThinMediaMultipleScattering);
            var line3 = GetPixelLine(Mode3D.ThinMediaSingleScattering);

            Vector3D min = new Vector3D(
                Math.Min(Math.Min(line1.Min(x => x.X), line2.Min(x => x.X)), line3.Min(x => x.X)),
                Math.Min(Math.Min(line1.Min(x => x.Y), line2.Min(x => x.Y)), line3.Min(x => x.Y)),
                Math.Min(Math.Min(line1.Min(x => x.Z), line2.Min(x => x.Z)), line3.Min(x => x.Z))
                );

            Vector3D max = new Vector3D(
                Math.Max(Math.Max(line1.Max(x => x.X), line2.Max(x => x.X)), line3.Max(x => x.X)),
                Math.Max(Math.Max(line1.Max(x => x.Y), line2.Max(x => x.Y)), line3.Max(x => x.Y)),
                Math.Max(Math.Max(line1.Max(x => x.Z), line2.Max(x => x.Z)), line3.Max(x => x.Z))
                );

            Vector3D rangeRgb = max - min;
            BitmapHelp.TransformBitmapListToCollum(new List<Bitmap>()
            {
                BitmapHelp.GetBitmapText("ThinMediaSingleScatteringBiased", 10, Color.Black, Color.White),
                GetPlotterImage(line1, rangeRgb),
                BitmapHelp.GetBitmapText("ThinMediaMultipleScattering", 10, Color.Black, Color.White),
                GetPlotterImage(line2, rangeRgb),
                BitmapHelp.GetBitmapText("ThinMediaSingleScattering", 10, Color.Black, Color.White),
                GetPlotterImage(line3, rangeRgb)
            }).Save(WorkingDirectory + "Cloud-SingleLine.bmp");
        }

        private Bitmap GetPlotterImage(Vector3D[] line, Vector3D rangeRgb)
        {
            return BitmapHelper.BitmapHelp.TransformBitmapListToRow(new List<Bitmap>()
            {
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.X).ToArray(), rangeRgb.X, "Red"),
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.Y).ToArray(), rangeRgb.Y, "Green"),
                PdfHistogram.FunctionPlotter.PlotFloatArray(line.Select(x => x.Z).ToArray(), rangeRgb.Z, "Blue"),
            });
        }

        private Vector3D[] GetPixelLine(Mode3D mode)
        {
            GraphicPanel3D graphic = new GraphicPanel3D() { Width = 420, Height = 328 };
            TestScenes.TestSzene18_CloudsForTestImage(graphic);
            graphic.Mode = mode;
            graphic.GlobalSettings.SamplingCount = mode == Mode3D.ThinMediaSingleScatteringBiased ? 1 : 10000;

            var image = graphic.GetRaytracingImageSynchron(graphic.Width, graphic.Height, new ImagePixelRange(351, 10, 1, 9));

            return image.RawImage.GetColum(0);
        }
    }
}

using System.Linq;
using GraphicMinimal;
using RaytracingColorEstimator;
using IntersectionTests;
using GraphicGlobal;
using RayCameraNamespace;
using FullPathGenerator;
using Radiosity._02_Patchcreation;
using Radiosity._03_ViewFactor;

//Forschungen\Wintersemeser_2016_2017_Santa_Monica\The_Radiosity_Algorithm_Basic_Implementa.pdf
//Ein Formfaktor Fij gibt an, wie viel Prozent der ausgehenden Lichtmenge von Patch i zum Patch j geht. Es wird also nicht ein einzelner Punkt auf Patch i oder j betrachet sondern die
//gesamten beiden Patchflächen. Die Brdf wird hierbei nicht beachtet. Beim beleuchten muss dann die beim Patch j eingehende Energie durch dessen Brdf abgeschwächt
//Die Summe aller Hemicube-Pixel-Faktoren muss genau 1 ergeben. Ich taste für Patch i über alle Hemicube-Pixels ab, wie viele Hemi-Strahlen Patch j treffen und Fij ist dann die Summe 
//der treffenden Pixel-Faktoren.

//Radiosity-Erklärung von mir für mich
//""""""""""""""""""""""""""""""""""""
//Man gibt für eine Lichtquelle mit der Emission an, wie viel Photonen sie aussenden soll. Unterteile ich die Lichtquelle in mehrere Patches, dann sendet jedes Patch 
//Patch-SurfaceArea / Patch-Sum-Von_Lichtquelle-SurfaceArea Photonen aus. Das Aussenden der Photonen erfolgt vom Patch-Centerpunkt. Man spannt über den Patch-Centerpunkt 
//ein Hemicube und schaut, wie viel Prozent der ausgesendeten Photonen jeweils in jede Hemicube-Richtung gesendet werden. Beim Empfängerpatch werden die Photonen, die es von
//jeden anderen Patch empfängt, aufsummiert. Die aufsummierten Photonen werden dann noch mit der Brdf gewichtet, bevor sie dann wieder erneut versendet werden.

//http://dudka.cz/rrv	-> DER Radiosity-Algorithmus

namespace Radiosity
{
    //Siehe Dokumentation.odt "Radiosity – SolidAngle vs Hemicube"
    public class Radiosity : IPixelEstimator
    {
        public enum Mode { SolidAngle, Hemicube };

        private readonly Mode mode = Mode.SolidAngle;

        private IRayCamera rayCamera;
        private IntersectionFinder intersectionFinder;

        public bool CreatesLigthPaths { get; } = false;

        public Radiosity(Mode modus)
        {
            this.mode = modus;
        }


        public void BuildUp(RaytracingFrame3DData data)
        {
            this.rayCamera = RayCameraFactory.CreateCamera(new CameraConstructorData() { Camera = data.GlobalObjektPropertys.Camera, ScreenWidth = data.ScreenWidth, ScreenHeight = data.ScreenHeight, PixelRange = data.PixelRange, DistanceDephtOfFieldPlane = data.GlobalObjektPropertys.DistanceDephtOfFieldPlane, WidthDephtOfField = data.GlobalObjektPropertys.WidthDephtOfField, DepthOfFieldIsEnabled = data.GlobalObjektPropertys.DepthOfFieldIsEnabled, SamplingMode = PixelSamplingMode.None });

            var settings = data.GlobalObjektPropertys.RadiositySettings;

            //Schritt 1: Patche erstellen
            var patches = PatchCreator.CreatePatchList(data, settings.MaxAreaPerPatch, settings.GenerateQuads, settings.SampleCountForPatchDividerShadowTest);

            this.intersectionFinder = new IntersectionFinder(patches.Cast<IIntersecableObject>().ToList(), data.ProgressChanged);

            foreach (var groupBy in patches.Where(x => x.IsLightSource).GroupBy(x => x.RayHeigh))
            {
                Vector3D centerRayHeigh = IntersectionHelper.GetBoundingBoxFromIVolumeObjektCollection(groupBy.Cast<IIntersecableObject>()).Center;
                foreach (var patch in groupBy)
                {
                    patch.SetCenterPointFromRayHeigh(centerRayHeigh);
                    patch.SetLightingArea(groupBy.Sum(x => x.SurfaceArea));
                }
            }            

            //Patche mit Zufallsfarbe ausgeben
            if (settings.RadiosityColorMode == RadiosityColorMode.RandomColors)
            {
                Vector3D[] colors = new Vector3D[] { new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1), new Vector3D(1, 1, 0), new Vector3D(0, 1, 1), new Vector3D(1, 0, 1), new Vector3D(1, 1, 1) };
                IRandom rand = new Rand(0);
                foreach (var patch in patches)
                {
                    patch.OutputRadiosity = colors[rand.Next(colors.Length)];
                    for (int i = 0; i < patch.CornerPoints.Length; i++)
                    {
                        patch.SetCornerColor(i, patch.OutputRadiosity);
                    }
                }
                return;
            }
            

            //Schritt 2: ViewFaktors berechnen
            var viewFaktorCalculator = new ViewFaktorCalculator(patches, this.intersectionFinder, settings.HemicubeResolution, settings.UseShadowRaysForVisibleTest, settings.VisibleMatrixFileName);
            if (mode == Mode.Hemicube)
            {
                viewFaktorCalculator.CalculateViewFaktorsWithHemicubeMethod(data.GlobalObjektPropertys.ThreadCount, data.ProgressChanged, data.StopTrigger);
            }
            else
            {
                viewFaktorCalculator.CalculateViewFaktorsWithSolidAngle(data.GlobalObjektPropertys.ThreadCount, data.ProgressChanged, data.StopTrigger);
            }

            //Schritt 3: Beleuchten
            PatchIlluminator.Illuminate(patches, settings.IlluminationStepCount, data.ProgressChanged);


            //Schritt 4: Farben interpolieren
            if (settings.RadiosityColorMode == RadiosityColorMode.WithColorInterpolation)
            {
                CornerColorCalculator.SetCornerColors(patches, (patch) => { return patch.OutputRadiosity; });
            }
            else
            {
                foreach (var patch in patches)
                {
                    for (int i = 0; i < patch.CornerPoints.Length; i++)
                    {
                        patch.SetCornerColor(i, patch.OutputRadiosity);
                    }
                }
            }
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            var primaryRay = this.rayCamera.CreatePrimaryRay(x, y, null);
            Vector3D color = GetPixelColor(primaryRay);
            if (color != null)
            {
                float pixelFilter = this.rayCamera.GetPixelPdfW(x, y, primaryRay.Direction);
                float cosAtCamera = this.rayCamera.UseCosAtCamera ? this.rayCamera.Forward * primaryRay.Direction : 1;
                color *= pixelFilter * cosAtCamera;
            }
            return new FullPathSampleResult() { RadianceFromRequestetPixel = color ?? new Vector3D(0, 0, 0), MainPixelHitsBackground = color == null };
        }

        private Vector3D GetPixelColor(Ray ray)
        {
            var point = this.intersectionFinder.GetIntersectionPoint(ray, 0);
            return point?.Color;
        }
    }
}


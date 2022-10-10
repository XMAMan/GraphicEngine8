using System;
using System.Text;
using FullPathGenerator;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using RayCameraNamespace;
using RayTracerGlobal;
using RaytracingBrdf.SampleAndRequest;
using SubpathGenerator;

namespace FullPathGeneratorTest.BasicTests.BasicTestHelper
{
    //Berechnet die geometrische Summe der Geometry- Brdf- und Attenuation-Werte
    class PathTroughputCalculator
    {
        private readonly float scatteringFromMedia;
        private readonly float absorbationFromMedia;
        private readonly float anisotrophyCoeffizient;
        private readonly RayCamera camera;
        private readonly int pixX;
        private readonly int pixY;
        private readonly float maxDistance; //Maximale Distance zwischen MediaLine-Endpoint und PathPoint, auf dem die MediaLine zeigt

        public PathTroughputCalculator(float scatteringFromMedia, float absorbationFromMedia, float anisotrophyCoeffizient, int pixX, int pixY, RayCamera camera, float maxDistance)
        {
            this.scatteringFromMedia = scatteringFromMedia;
            this.absorbationFromMedia = absorbationFromMedia;
            this.anisotrophyCoeffizient = anisotrophyCoeffizient;
            this.pixX = pixX;
            this.pixY = pixY;
            this.camera = camera;
            this.maxDistance = maxDistance;            
        }

        public float GetPathtroughput(FullPathPoint[] points)
        {
            return GetPathtroughputFromIndexToIndex(points, 0, points.Length - 1);
        }

        public float GetPathtroughputFromIndexToIndex(FullPathPoint[] points, int startIndex, int endIndex)
        {
            StringBuilder debugLog = new StringBuilder();

            float geometrySum = 1;

            if (startIndex == 0)
            {
                float pixelFilter = this.camera.GetPixelPdfW(this.pixX, this.pixY, Vector3D.Normalize(points[1].Position - points[0].Position));
                geometrySum *= pixelFilter;
                debugLog.AppendLine($"* pixelFilter={pixelFilter}");
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                debugLog.AppendLine($"P[{i}]={points[i].Position}\tP[{i + 1}]={points[i + 1].Position}");
                float geometryTerm = GeometryTerm(points, i, i+1, debugLog);
                geometrySum *= geometryTerm;
                if (i > 0)
                {
                    Vector3D outputDirection = Vector3D.Normalize(points[i + 1].Position - points[i].Position);
                    var brdf = points[i].Point.BrdfPoint.Evaluate(outputDirection);
                    debugLog.AppendLine($"* DiffuseBrdf[{i}]={brdf.Brdf.X}");
                    geometrySum *= brdf.Brdf.X;
                }
            }

            debugLog.AppendLine($"== geometrySum={geometrySum}");
            string debugString = debugLog.ToString();

            return geometrySum;
        }

        private static float GeometryTerm(FullPathPoint[] points, int i1, int i2, StringBuilder debugLog)
        {
            PathPoint p1 = points[i1].Point;
            PathPoint p2 = points[i2].Point;

            Vector3D dir = Vector3D.Normalize(p2.Position - p1.Position);
            float r2 = Math.Max((p2.Position - p1.Position).SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            debugLog.AppendLine($"/ Distance[{i1}-{i2}]={r2}");
            debugLog.AppendLine($"* CosP1[{i1}]={Math.Abs(p1.Normal * dir)}");
            debugLog.AppendLine($"* CosP2[{i2}]={Math.Abs(p2.Normal * (-dir))}");
            return Math.Abs(p1.Normal * dir) / r2 * Math.Abs(p2.Normal * (-dir));
        }

        public float GetPathtroughputWithMedia(FullPathPoint[] points)
        {
            return GetPathtroughputFromIndexToIndexWithMedia(points, 0, points.Length - 1);
        }

        public float GetPathtroughputFromIndexToIndexWithMedia(FullPathPoint[] points, int startIndex, int endIndex)
        {
            StringBuilder debugLog = new StringBuilder();

            float geometrySum = 1;

            if (startIndex == 0)
            {
                float pixelFilter = this.camera.GetPixelPdfW(this.pixX, this.pixY, Vector3D.Normalize(points[1].Position - points[0].Position));
                geometrySum *= pixelFilter;
                debugLog.AppendLine($"* pixelFilter={pixelFilter:G9}");
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                debugLog.AppendLine($"P[{i}]={points[i].Position}\tP[{i + 1}]={points[i + 1].Position}");
                float geometryTerm = GeometryTermWithMedia(points, i, i + 1, debugLog);
                geometrySum *= geometryTerm;

                if (i > 0)
                {
                    if (points[i].LocationType == MediaPointLocationType.Surface)
                    {
                        float diffuseBrdf = points[i].Point.SurfacePoint.Color.X / (float)Math.PI * points[i].Point.SurfacePoint.Propertys.Albedo;
                        debugLog.AppendLine($"* DiffuseBrdf[{i}]={diffuseBrdf:R}");
                        geometrySum *= diffuseBrdf; //Diffuse Brdf
                    }
                    else if (points[i].LocationType == MediaPointLocationType.NullMediaBorder)
                    {
                        float diffuseBrdf = 1;
                        debugLog.AppendLine($"* Air-Media-Border[{i}]={diffuseBrdf:R}");
                        geometrySum *= diffuseBrdf; //Air-Media-Border Brdf
                    }
                    else if (points[i].LocationType == MediaPointLocationType.MediaParticle)
                    {
                        float phaseFunction;
                        if (this.anisotrophyCoeffizient == 0)
                        {
                            phaseFunction = 1 / (float)(4 * Math.PI); //Bei ein Homogeonen Medium mit Anisotrophicfactor von 0 wird die Isotrophic-Phasenfunktion genutzt.
                        }else
                        {
                            phaseFunction = HenyeyGreensteinPhaseFunction(points, i, this.anisotrophyCoeffizient);
                        }
                        float phaseWithOs = phaseFunction * this.scatteringFromMedia;
                        debugLog.AppendLine($"* PhaseScatteringBsdf[{i}]={phaseWithOs:R}");
                        geometrySum *= phaseWithOs; //Phasefunction * Media-Scattering-Term
                    }
                }

                float attenuation = Attenuation(points, i, i + 1, this.scatteringFromMedia, this.absorbationFromMedia);
                _ = debugLog.AppendLine($"* attenuation[{i}-{i + 1}]={attenuation:R}");
                geometrySum *= attenuation;
                debugLog.AppendLine($"== geometrySum[{startIndex}-{i + 1}]={geometrySum:R}");
            }

            debugLog.AppendLine($"== geometrySum={geometrySum:R}");
            string debugString = debugLog.ToString();
            return geometrySum;
        }

        private static float HenyeyGreensteinPhaseFunction(FullPathPoint[] points, int i1, float anisotropyCoeffizient)
        {
            PathPoint p1 = points[i1 - 1].Point;
            PathPoint p2 = points[i1].Point;
            PathPoint p3 = points[i1 + 1].Point;

            Vector3D directionToBrdfPoint = Vector3D.Normalize(p2.Position - p1.Position);
            Vector3D outDirection = Vector3D.Normalize(p3.Position - p2.Position);

            float cosTheta = directionToBrdfPoint * outDirection;
            float squareMeanCosine = anisotropyCoeffizient * anisotropyCoeffizient;
            float d = 1 + squareMeanCosine - (anisotropyCoeffizient + anisotropyCoeffizient) * cosTheta;

            return d > 0 ? (float)((1.0f / (4 * Math.PI) * (1 - squareMeanCosine) / (d * Math.Sqrt(d)))) : 0;
        }

        private static float GeometryTermWithMedia(FullPathPoint[] points, int i1, int i2, StringBuilder debugLog)
        {
            PathPoint p1 = points[i1].Point;
            PathPoint p2 = points[i2].Point;

            Vector3D dir = Vector3D.Normalize(p2.Position - p1.Position);
            float r2 = Math.Max((p2.Position - p1.Position).SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            debugLog.AppendLine($"/ Distance[{i1}-{i2}]={r2:R}");
            float sum = 1.0f / r2;
            if (p1.LocationType == MediaPointLocationType.Surface || p1.LocationType == MediaPointLocationType.Camera || p1.LocationType == MediaPointLocationType.MediaBorder)
            {
                sum *= Math.Abs(p1.Normal * dir); //Der Kamerapuntk ist auch nciht auf ein Surface, hat aber kein RayHeigh im Gegensatz zu ein Mediapunkt
                debugLog.AppendLine($"* CosP1[{i1}]={Math.Abs(p1.Normal * dir):R}");
            }
            if (p2.LocationType == MediaPointLocationType.Surface || p2.LocationType == MediaPointLocationType.Camera || p2.LocationType == MediaPointLocationType.MediaBorder)
            {
                sum *= Math.Abs(p2.Normal * (-dir));
                debugLog.AppendLine($"* CosP2[{i2}]={Math.Abs(p2.Normal * (-dir)):R}");
            }
            return (float)sum;
        }
        //Ich gehe davon aus, dass von jeden Segment RayMin,RayMax und HasScattering korrekt sind
        private float Attenuation(FullPathPoint[] points, int i1, int i2, float scatteringFromMedia, float absorbationFromMedia)
        {
            FullPathPoint p1 = points[i1];
            FullPathPoint p2 = points[i2];

            MediaLine mediaLine = null;
            if (p1.EyeLineToNext != null && (p1.EyeLineToNext.EndPoint.Position - p2.Position).Length() < maxDistance) mediaLine = p1.EyeLineToNext;
            else
            if (p2.LightLineToNext != null && (p2.LightLineToNext.EndPoint.Position - p1.Position).Length() < maxDistance) mediaLine = p2.LightLineToNext;

            float attenuation = 1;
            //foreach (var s in mediaLine.Segments)
            for (int i = 0; i < mediaLine.ShortRaySegmentCount; i++)
            {
                var s = mediaLine.Segments[i];

                //Während der SubPath-Erstellung hab ich z.B. eine MediaLine, welche von 0 bis 100 im Vacuum und von 100 bis d im Medium verläuft
                //Die Attenuation während der SubPatherstellunge wird nun über 100*Scatter und d*Scatter berechnet. D.h. RayMin beginnt immer bei 0
                //Erst beim zusammensetzen zu einer Line mit 2 Segmenten hab ich dann den Fall, dass RayMin beim zweiten Segment bei ein Wert größer 0 beginnt
                //Leider ist (100+d) - 100 != d wegen Floatungeneuigkeiten. Deswegen nehme ich hier die Attenuation aus dem SubPath anstatt sie neu über
                //veränderte RayMin/RayMax-Werte zu berechnen
                //if (s.Media.HasScatteringSomeWhereInMedium()) attenuation *= (float)Math.Exp(-(scatteringFromMedia + absorbationFromMedia) * (s.RayMax - s.RayMin));
                if (s.Media.HasScatteringSomeWhereInMedium()) attenuation *= s.Attenuation.X;
            }

            return attenuation;
        }

        
    }
}

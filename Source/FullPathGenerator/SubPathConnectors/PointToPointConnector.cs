using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RayCameraNamespace;
using RayTracerGlobal;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using RaytracingLightSource;
using SubpathGenerator;
using System;

namespace FullPathGenerator
{
    //Verbindet zwei PathPoints miteinander, wenn sie sich sehen (Das sind alles Unbiased FullPath-Erzeugungsmethoden (Neben Pathtracing))
    //LightTracing,DirectLighting,VertexConnection
    public class PointToPointConnector
    {
        private readonly RayVisibleTester visibleTester;
        private readonly IRayCamera rayCamera;
        private readonly MediaIntersectionPoint cameraMediaPoint = null; //An diesen Punkt befindet sich die Kamera
        private readonly bool noDistanceSampling;
        public readonly IPhaseFunctionSampler PhaseFunction;

        public PointToPointConnector(RayVisibleTester visibleTester, IRayCamera rayCamera, PathSamplingType usedSubPathType, IPhaseFunctionSampler phaseFunction)
        {
            this.noDistanceSampling = usedSubPathType == PathSamplingType.ParticipatingMediaWithoutDistanceSampling;
            this.visibleTester = visibleTester;
            this.rayCamera = rayCamera;
            this.PhaseFunction = phaseFunction;

            if (this.visibleTester.ContainsMedia)
            {
                this.cameraMediaPoint = visibleTester.CreateCameraMediaStartPoint(rayCamera.Position);                
            }            
        }

        //DirectLighting
        public EyePoint2LightSourceConnectionData TryToConnectToLightSource(PathPoint eyePoint, DirectLightingSampleResult toLightDirection)
        {
            if (this.visibleTester.ContainsMedia == false) //Ohne Media. eyePoint ist somit Surface-Punkt
            {
                if (eyePoint.Normal * toLightDirection.DirectionToLightPoint <= 0) return null;

                //Shadow-Ray-Test (Muss ich leider vor dem Brdf-VisibleTest machen, da ich noch die toLightDirection korrigieren muss)
                var lightPoint = this.visibleTester.GetPointOnLightsource(eyePoint.SurfacePoint, eyePoint.AssociatedPath.PathCreationTime, toLightDirection);
                if (lightPoint == null) return null;

                //Brdf-VisibleTest 
                toLightDirection.DirectionToLightPoint = Vector3D.Normalize(lightPoint.Position - eyePoint.Position); //Brdf-VisibleTest -> Damit der A_PathLengthOK_PathPdfA_PathContribution ein PdfA-Error von 0 bringt korrigiere ich die Direction
                var eyeBrdf = eyePoint.BrdfPoint.Evaluate(toLightDirection.DirectionToLightPoint);
                if (eyeBrdf == null) return null;

                //GeometryTerm
                float distanceSqr = Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, (lightPoint.Position - eyePoint.Position).SquareLength());
                float cosLight = (-toLightDirection.DirectionToLightPoint) * lightPoint.ShadedNormal;
                if (toLightDirection.LightSourceIsInfinityAway) distanceSqr = 1; //Richtungslicht was aus dem unendlichen kommt soll immer gleichhell/stark an alle Szenenpunkte leuchten. Dessen Abstand soll keine Rolle spielen
                float geometryTherm = cosLight * eyeBrdf.CosThetaOut / distanceSqr;

                return new EyePoint2LightSourceConnectionData()
                {
                    GeometryTerm = geometryTherm,
                    AttenuationTerm = new Vector3D(1,1,1),
                    EyeBrdf = eyeBrdf,
                    LightPoint = lightPoint,
                    DirectionToLightPoint = toLightDirection.DirectionToLightPoint,
                    PdfLForEyepointToLightSource = new DistancePdf() { PdfL = 1, ReversePdfL = 1 },
                    LighIsInfinityAway = toLightDirection.LightSourceIsInfinityAway
                };
            }
            else
            {
                if (eyePoint.LocationType == MediaPointLocationType.Surface && eyePoint.Normal * toLightDirection.DirectionToLightPoint <= 0) return null;

                //Brdf-VisibleTest -> Der gesampelte LightSource-Punkt entspricht nicht 100% dem per ShadowRayTest erzeugten Punkt. 
                //                    Deswegen weicht toLightDirection.DirectionToLightPoint etwas von Vector3D.Normalize(toLightMediaLine.EndPoint.Position - eyePoint.Position) ab
                //                    Diese Abweichung führt zu einer PdfW/GeometryTerm-Abweichung, welche dann beim A_PathLengthOK_PathPdfA_PathContribution-Test auffällt

                //Shadow-Ray-Test (Muss ich leider vor dem Brdf-VisibleTest machen, da ich noch die toLightDirection korrigieren muss)
                var toLightMediaLine = this.visibleTester.GetLineToLightSource(eyePoint.MediaPoint, eyePoint.AssociatedPath.PathCreationTime, toLightDirection);
                if (toLightMediaLine == null) return null;

                //Brdf-VisibleTest -> Bei dieser Variante ist der PfadPdfA-Error beim A_PathLengthOK_PathPdfA_PathContribution-Test 0
                toLightDirection.DirectionToLightPoint = Vector3D.Normalize(toLightMediaLine.EndPoint.Position - eyePoint.Position);
                var eyeBrdf = BrdfFromPathPoint(eyePoint.DirectionToThisPoint, eyePoint, toLightDirection.DirectionToLightPoint);
                if (eyeBrdf == null) return null;

                //GeometryTerm
                float distanceSqr = Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, (toLightMediaLine.EndPoint.Position - eyePoint.Position).SquareLength());
                float cosLight = (-toLightDirection.DirectionToLightPoint) * toLightMediaLine.EndPoint.SurfacePoint.ShadedNormal;
                if (toLightDirection.LightSourceIsInfinityAway) distanceSqr = 1; //Richtungslicht was aus dem unendlichen kommt soll immer gleichhell/stark an alle Szenenpunkte leuchten. Dessen Abstand soll keine Rolle spielen
                float geometryTherm = cosLight * eyeBrdf.CosThetaOut / distanceSqr; //Bei Mediapunkten ist der Cos-Term bei der Brdf-Funktion 1 (CosThetaOut wird von der Phasenfunktion auf 1 gesetzt)

                return new EyePoint2LightSourceConnectionData()
                {
                    GeometryTerm = geometryTherm,
                    AttenuationTerm = toLightMediaLine.AttenuationWithoutPdf(),
                    EyeBrdf = eyeBrdf,
                    LightPoint = toLightMediaLine.EndPoint.SurfacePoint,
                    MediaLightPoint = toLightMediaLine.EndPoint,
                    DirectionToLightPoint = toLightDirection.DirectionToLightPoint,
                    LineFromEyePointToLightSource = toLightMediaLine,
                    PdfLForEyepointToLightSource = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : toLightMediaLine.GetPdfLIfDistanceSamplingWouldBeUsed(),
                    LighIsInfinityAway = toLightDirection.LightSourceIsInfinityAway
                };
            }
        }
        //Lighttracing        
        public LightPoint2CameraConnectionData TryToConnectToCamera(PathPoint lightPoint)
        {
            Vector3D cameraToLightPoint = lightPoint.Position - this.rayCamera.Position;
            float cameraToLightPointDistance = cameraToLightPoint.Length();
            Vector3D cameraToLightPointDirection = cameraToLightPoint / cameraToLightPointDistance;

            //Brdf-VisibleTest
            float cameraCos = this.rayCamera.UseCosAtCamera ? this.rayCamera.Forward * cameraToLightPointDirection : 1;
            if (cameraCos <= 0) return null; //Lightpoint-Normale zeigt nicht zu Kamera

            //View-Frustum-Test
            var pixelPosition = this.rayCamera.GetPixelPositionFromEyePoint(lightPoint.Position);
            if (pixelPosition == null) return null; //lightPoint liegt außerhalb des Kamera-View-Frustums

            if (this.visibleTester.ContainsMedia == false) //Ohne Media. eyePoint ist somit Surface-Punkt
            {
                //Brdf-VisibleTest
                var lightBrdf = lightPoint.BrdfPoint.Evaluate(-cameraToLightPointDirection);
                if (lightBrdf == null) return null; //Brdf läßt nichts durch

                //Shadow-Ray-Test
                bool isVisible = this.visibleTester.IsCameraVisible(lightPoint.SurfacePoint, lightPoint.AssociatedPath.PathCreationTime, this.rayCamera.Position);
                if (isVisible == false) return null;

                //GeometryTerm
                float geomertryFaktor = (cameraCos * lightBrdf.CosThetaOut) / Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, (lightPoint.Position - this.rayCamera.Position).SquareLength());

                return new LightPoint2CameraConnectionData()
                {
                    GeometryTerm = geomertryFaktor,
                    AttenuationTerm = new Vector3D(1, 1, 1),
                    LightBrdf = lightBrdf,
                    CameraToLightPointDirection = cameraToLightPointDirection,
                    PixelPosition = pixelPosition,                    
                    PdfLForCameraToLightPoint = new DistancePdf() { PdfL = 1, ReversePdfL = 1 },
                    CameraPoint = PathPoint.CreateCameraPoint(this.rayCamera.Position, this.rayCamera.Forward, null)
                };
            }else
            {
                //Brdf-VisibleTest
                var lightBrdf = BrdfFromPathPoint(lightPoint.DirectionToThisPoint, lightPoint, -cameraToLightPointDirection);
                if (lightBrdf == null) return null; //Brdf läßt nichts durch

                //Shadow-Ray-Test (Ich muss hier von der Kamera zum Lightpoint laufen, da die MediaLine genau in dieser Richtung erwartet wird)
                var mediaLine = this.visibleTester.GetLineFromCameraToLightPoint(this.cameraMediaPoint, cameraToLightPointDirection, cameraToLightPointDistance, lightPoint);
                if (mediaLine == null) return null;

                //Bei Mediapunkten ist der Cos-Term bei der Brdf-Funktion 1
                float geomertryFaktor = (cameraCos * lightBrdf.CosThetaOut) / Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, (lightPoint.Position - this.rayCamera.Position).SquareLength());

                return new LightPoint2CameraConnectionData()
                {
                    GeometryTerm = geomertryFaktor,
                    AttenuationTerm = mediaLine.AttenuationWithoutPdf(),
                    LightBrdf = lightBrdf,
                    CameraToLightPointDirection = cameraToLightPointDirection,
                    PixelPosition = pixelPosition,
                    LineFromCameraToLightPoint = mediaLine,
                    PdfLForCameraToLightPoint = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : mediaLine.GetPdfLIfDistanceSamplingWouldBeUsed(),
                    CameraPoint = PathPoint.CreateCameraPoint(new MediaIntersectionPoint(this.cameraMediaPoint), this.rayCamera.Forward, null)
                };
            }                
        }

        //VertexConnection
        public EyePoint2LightPointConnectionData TryToConnectTwoPoints(PathPoint eyePoint, PathPoint lightPoint)
        {
            //Zeigen dA-Flächen von Eye- und Light-Point zueinander?
            Vector3D point1ToPoint2 = lightPoint.Position - eyePoint.Position;
            float point1ToPoint2Distance = point1ToPoint2.Length();
            //if (point1ToPoint2Distance < MagicNumbers.MinAllowedPathPointDistance) return null;
            Vector3D point1ToPoint2Direction = point1ToPoint2 / point1ToPoint2Distance;

            if (this.visibleTester.ContainsMedia == false) //Ohne Media. eyePoint ist somit Surface-Punkt
            {
                //Brdf-VisibleTest
                var eyeBrdf = eyePoint.BrdfPoint.Evaluate(point1ToPoint2Direction);//VertexConnection-Eye
                if (eyeBrdf == null) return null;
                var lightBrdf = lightPoint.BrdfPoint.Evaluate(-point1ToPoint2Direction);//VertexConnection-Light
                if (lightBrdf == null) return null;

                //Visible Test zwischen Eye- und Light-Point
                if (this.visibleTester.IsVisibleFromSurfaceToSurface(eyePoint.SurfacePoint, eyePoint.AssociatedPath.PathCreationTime, point1ToPoint2Direction, lightPoint.SurfacePoint) == false) 
                    return null;

                //GeometryTerm
                float geomertryFaktor = Math.Abs(eyeBrdf.CosThetaOut * lightBrdf.CosThetaOut) / Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, point1ToPoint2.SquareLength());

                return new EyePoint2LightPointConnectionData()
                {
                    GeometryTerm = geomertryFaktor,
                    AttenuationTerm = new Vector3D(1, 1, 1),
                    EyeBrdf = eyeBrdf,
                    LightBrdf = lightBrdf,
                    PdfLForEyeToLightPoint = new DistancePdf() { PdfL = 1, ReversePdfL = 1 }
                };
            }else
            {
                //Brdf-VisibleTest
                var eyeBrdf = BrdfFromPathPoint(eyePoint.DirectionToThisPoint, eyePoint, point1ToPoint2Direction);
                if (eyeBrdf == null) return null;
                var lightBrdf = BrdfFromPathPoint(lightPoint.DirectionToThisPoint, lightPoint, -point1ToPoint2Direction);
                if (lightBrdf == null) return null;

                //Visible Test zwischen Eye- und Light-Point
                var mediaLine = this.visibleTester.GetLineFromP1ToP2(eyePoint, point1ToPoint2Direction, point1ToPoint2Distance, lightPoint);
                if (mediaLine == null) return null;

                //Bei Mediapunkten ist der Cos-Term bei der Brdf-Funktion 1
                float geomertryFaktor = Math.Abs(eyeBrdf.CosThetaOut * lightBrdf.CosThetaOut) / Math.Max(MagicNumbers.MinAllowedPathPointSqrDistance, point1ToPoint2.SquareLength());

                return new EyePoint2LightPointConnectionData()
                {
                    GeometryTerm = geomertryFaktor,
                    AttenuationTerm = mediaLine.AttenuationWithoutPdf(),
                    EyeBrdf = eyeBrdf,
                    LightBrdf = lightBrdf,
                    LineFromEyeToLightPoint = mediaLine,
                    PdfLForEyeToLightPoint = this.noDistanceSampling ? new DistancePdf() { PdfL = 1, ReversePdfL = 1 } : mediaLine.GetPdfLIfDistanceSamplingWouldBeUsed()
                };
            }                
        }

        //GetPathPdfAForAGivenPath bei DirectLightingOnEdge/LighTracingOnEdge
        //Erzeugt eine Long-Ray-Linie, welche vom StartPoint in Richtung particlePoint geht und dessen ShortRay-EndPoint auf dem particlePoint endet aber was danach noch weiter bis zur nächsten MediaBorder/Surface/Infinity geht
        public MediaLine CreateLongLineWithEndPointOnGivenParticle(PathPoint startPoint, PathPoint particlePoint)
        {
            Vector3D toParticleDirection = Vector3D.Normalize(particlePoint.Position - startPoint.Position);
            var mediaLine = this.visibleTester.MediaIntersectionFinder.CreateMediaShortLineNoAirBlocking(new MediaIntersectionPoint(startPoint.MediaPoint), toParticleDirection, float.MaxValue, startPoint.AssociatedPath.PathCreationTime);
            if (mediaLine == null) return null; //Wenn das getroffene Objekt zu vom Startpunkt entfernt ist, kann keine MediaLine erstellt werden
            float distanceToParticle = (particlePoint.Position - startPoint.Position).Length();
            if (distanceToParticle >= mediaLine.ShortRayLength) return null; //Wenn Particle zu kleinen Abstand zur dahinter liegenden Wand hat dann ist LongRay-Erzeugung nicht möglich
            return mediaLine.CreateLongMediaSubLine(distanceToParticle);
        }

        private BrdfEvaluateResult BrdfFromPathPoint(Vector3D directionToBrdfPoint, PathPoint brdfPoint, Vector3D outDirection)
        {
            if (brdfPoint.LocationType == MediaPointLocationType.Surface)
            {
                return brdfPoint.BrdfPoint.Evaluate(outDirection);
            }
            else
            {
                return this.PhaseFunction.EvaluateBsdf(directionToBrdfPoint, brdfPoint.MediaPoint, outDirection);
            }
        }
    }
}

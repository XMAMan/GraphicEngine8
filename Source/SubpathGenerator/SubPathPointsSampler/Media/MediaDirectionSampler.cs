using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;

namespace SubpathGenerator.SubPathPointsSampler.Media
{
    class MediaDirectionSampler
    {
        private readonly IBrdfSampler standardBrdfSampler;
        private readonly IPhaseFunctionSampler phaseFunction;

        public MediaDirectionSampler(IBrdfSampler standardBrdfSampler, IPhaseFunctionSampler phaseFunction)
        {
            this.standardBrdfSampler = standardBrdfSampler;
            this.phaseFunction = phaseFunction;
        }

        public BrdfSampleEvent SampleDirection(PathPoint pathPoint, MediaRayWalkData rayWalkData, IRandom rand)
        {
            var newDirection = SampleDirectionOnPathPoint(pathPoint, rand);
            if (newDirection == null) return null; //Abbruch, da Photon absorbiert wurde

            if (newDirection.RayWasRefracted && rayWalkData.IsEyePath) //Wurde Mediumrand (Glas oder Wolke) durchlaufen?
            {
                //Nehmen wir mal an per Lighttracing wird 80% des Lichts gebrochen. D.h. aus Lightracing-Richtung gehen 100 Photonen rein
                //und 80 Photonen kommen an der anderen Glasseite raus. Wenn ich den Pathtracing-Pfad berechne, dann gehen 80 Photonen rein
                //und es müssen 100 Photonen an der anderen Seite rauskommen, damit Pathtracing == Lighttracing.
                //Physikalisch genommen gibt es ja nur Lightracing und Pathtracing ist eher ein Trick, wo ich das Lighttracing rückwärts gehe.
                //Deswegen ist es auch ok, dass das Pathgewicht beim Brechen hier mehr wird.
                //Beim Eindringen ins Medium wird das Pathtracing-Pfadgewicht kleiner und beim rausgehen um den selben Faktor wieder größer.
                float relativeIOR = (rayWalkData.RefractionIndexCurrentMedium / rayWalkData.RefractionIndexNextMedium); //SmallUPBP->Bsdf.hxx Zeile 530
                newDirection.Brdf *= (relativeIOR * relativeIOR);       //Wenn ich diese Zeile nicht drin habe, ist die Kerze und der Eisblock heller und sieht weniger dem SmallUPBP-Bild ähnlich
            }

            return newDirection;
        }

        private BrdfSampleEvent SampleDirectionOnPathPoint(PathPoint point, IRandom rand)
        {
            if (point.LocationType == MediaPointLocationType.MediaBorder && point.MediaPoint.ThereIsNoMediaChangeAfterCrossingBorder()) //Glaswürfel mit niedrigerer Prio
            {
                return new BrdfSampleEvent()
                {
                    Brdf = new Vector3D(1, 1, 1),
                    ExcludedObject = point.MediaPoint.SurfacePoint.IntersectedObject,
                    IsSpecualarReflected = true,
                    PdfW = 1,
                    PdfWReverse = 1,
                    Ray = new Ray(point.Position, point.DirectionToThisPoint),
                    RayWasRefracted = true,
                };
            }

            if (point.LocationType == MediaPointLocationType.Surface || point.LocationType == MediaPointLocationType.MediaBorder) //Oberfläche (Diffuse/Glas)
            {
                return point.BrdfPoint.SampleDirection(this.standardBrdfSampler, rand);
            }
            else
            {
                return this.phaseFunction.SampleDirection(point.MediaPoint, point.DirectionToThisPoint, rand);
            }
        }
    }
}

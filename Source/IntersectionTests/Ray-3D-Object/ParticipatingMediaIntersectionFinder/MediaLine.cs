using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;
using RayTracerGlobal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntersectionTests
{
    //Eine Linie zwischen zwei MediaIntersectionPoints
    //Wenn man ein Short-Ray hat, dann ist der Media-Endpunkt am Ende des letzten Volumensegments
    //Hat man ein Long-Ray, dann gehen die Volumensegmetne noch weiter bis zum nächsten Surfacepunkt und der Media-Endpoint liegt in ein bliebigen Segment davor
    public class MediaLine : IQueryLine
    {
        public MediaIntersectionPoint StartPoint { get; private set; } //Startpunkt des Rays
        public MediaIntersectionPoint EndPoint { get; private set; }   //(ShortRay)Endpunkt
        public List<VolumeSegment> Segments { get; private set; }
        public Ray Ray { get; private set; } //Startpunkt + Richtung
        public float ShortRayLength { get; private set; } //Die Länge der Linie vom startPoint bis zum endPoint
        public float LongRayLength { get; private set; } //Über die Länge der Linie hinausgehend zeigt sie hier zum nächsten Surface-Punkt
        public MediaPointLocationType EndPointLocation { get { return this.EndPoint.Location; } } //Das passiert, wenn der Strahl das Medium verläßt und ins unendliche wegfliegt. Wenn er niemals in ein Medium war, dann kommt null zurück
        public int ShortRaySegmentCount { get; private set; } //Bis zu diesen Index gehen die Short-Ray-Segmente (D.h. alle Segemnte, die vor dem EndPoint liegen)

        private MediaLine(Ray ray, MediaIntersectionPoint startPoint, MediaIntersectionPoint endPoint, List<VolumeSegment> segments, int shortRaySegmentCount)
        {
            if (shortRaySegmentCount == 0) throw new ArgumentException("shortRaySegmentCount must be greater than zero");

            this.ShortRaySegmentCount = shortRaySegmentCount;
            this.Ray = ray;
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
            this.Segments = segments;

            this.ShortRayLength = (endPoint.Position - ray.Start).Length();
            this.LongRayLength = segments.Last().RayMax;
        }

        //Liegt eines der Segmente in ein Medium mit Partikeln?
        public bool HasScattering()
        {
            return this.Segments.Any(x => x.Media.HasScatteringSomeWhereInMedium());
        }

        //Hinter dem EndPoint kommen keine weiteren Segmente
        public static MediaLine CreateShortRayLine(Ray ray, MediaIntersectionPoint startPoint, MediaIntersectionPoint endPoint, List<VolumeSegment> segments)
        {
            return new MediaLine(ray, startPoint, endPoint, segments, segments.Count);
        }

        //Hinter dem EndPoint kommen weitere Segmente
        public static MediaLine CreateLongRayLine(Ray ray, MediaIntersectionPoint startPoint, MediaIntersectionPoint endPoint, List<VolumeSegment> segments, int shortRaySegmentCount)
        {
            return new MediaLine(ray, startPoint, endPoint, segments, shortRaySegmentCount);
        }

        public static MediaLine CreateOneSegmentLine(Ray ray, MediaIntersectionPoint startPoint, MediaIntersectionPoint endPoint, RaySampleResult sampleResult)
        {
            return new MediaLine(ray, startPoint, endPoint, new List<VolumeSegment>() { new VolumeSegment(ray, startPoint, sampleResult, 0, sampleResult.RayPosition) }, 1);
        }

        public Vector3D AttenuationWithoutPdf()
        {
            Vector3D attenuation = new Vector3D(1, 1, 1);
            for (int i=0;i<this.ShortRaySegmentCount;i++)
            {
                attenuation = Vector3D.Mult(attenuation, this.Segments[i].Attenuation);
            }
            return attenuation;
        }

        //Mit dieser PdfL wurde die MediaLine gesampelt (Wenn DistanceSampling == false, dann steht hier immer 1)
        public DistancePdf SampledPdfL()
        {
            DistancePdf pdf = new DistancePdf()
            {
                PdfL = 1,
                ReversePdfL = 1
            };

            for (int i = 0; i < this.ShortRaySegmentCount; i++)
            {
                var segment = this.Segments[i];
                pdf.PdfL *= segment.PdfL;
                pdf.ReversePdfL *= segment.PdfLReverse;
            }
            return pdf;
        }

        //Gibt die PdfL zurück, welche man hätte, wenn man Distancesampling nutzen würde
        public DistancePdf GetPdfLIfDistanceSamplingWouldBeUsed()
        {
            DistancePdf pdf = new DistancePdf()
            {
                PdfL = 1,
                ReversePdfL = 1
            };
            for (int i = 0; i < this.ShortRaySegmentCount; i++)
            {
                var segment = this.Segments[i];
                bool startPointIsOnParticleInMedium = segment == this.Segments.First() && this.StartPoint.Location == MediaPointLocationType.MediaParticle;
                bool endPointIsOnParticleInMedium = (i == this.ShortRaySegmentCount - 1) && this.EndPoint.Location == MediaPointLocationType.MediaParticle;

                var result = segment.Media.DistanceSampler.GetSamplePdfFromRayMinToInfinity(this.Ray, segment.RayMin, segment.RayMax, this.ShortRayLength, startPointIsOnParticleInMedium, endPointIsOnParticleInMedium);
                pdf.PdfL *= result.PdfL;
                pdf.ReversePdfL *= result.ReversePdfL;
            }
            return pdf;
        }

        //Berechnet die Attenuation (Pfaddurchsatz) von Ray-Start bis Ray-Start + distanceToRayStart
        public Vector3D AttenuationWithoutPdf(float distanceToRayStart)
        {
            Vector3D attenuation = new Vector3D(1, 1, 1);
            for (int i = 0; i < this.ShortRaySegmentCount; i++)
            {
                var segment = this.Segments[i];
                if (distanceToRayStart > segment.RayMin)
                {
                    attenuation = Vector3D.Mult(attenuation, segment.EvaluateAttenuation(distanceToRayStart));
                }
                else
                {
                    break;
                }
            }
            return attenuation;
        }

        public Vector3D AttenuatedEmissionWithoutPdf()
        {
            Vector3D attenuation = new Vector3D(1, 1, 1);
            Vector3D emission = new Vector3D(0, 0, 0);
            for (int i = 0; i < this.ShortRaySegmentCount; i++)
            {
                var segment = this.Segments[i];
                attenuation = Vector3D.Mult(attenuation, segment.Attenuation);
                emission += Vector3D.Mult(segment.Emission, attenuation);
            }
            return emission;
        }

        public Vector3D AttenuatedEmissionWithPdf()
        {
            Vector3D attenuation = new Vector3D(1, 1, 1);
            Vector3D emission = new Vector3D(0, 0, 0);
            float pdf = 1;
            for (int i = 0; i < this.ShortRaySegmentCount; i++)
            {
                var segment = this.Segments[i];
                attenuation = Vector3D.Mult(attenuation, segment.Attenuation);
                pdf *= segment.PdfL;
                emission += Vector3D.Mult(segment.Emission, attenuation) / pdf;
            }
            return emission;
        }        

        public IParticipatingMedia GetMedia(float distanceToRayStart)
        {
            foreach (var segment in this.Segments)
            {
                if (distanceToRayStart > segment.RayMin && distanceToRayStart <= segment.RayMax)
                {
                    return segment.Media;
                }
            }

            throw new Exception("distanceToRayStart liegt in keinen der Segmentbereiche. distanceToRayStart = " + distanceToRayStart + " Segmentbereiche = " + string.Join("|", this.Segments.Select(x => "RayMin = " + x.RayMin + ", RayMax = " + x.RayMax)));
        }

        public Vector3D GetPositionFromDistance(float distanceToRayStart)
        {
            return this.Ray.Start + this.Ray.Direction * distanceToRayStart;
        }

        public MediaLine CreateShortMediaSubLine(float distanceToRayStart)
        {
            return CreateMediaSubLine(distanceToRayStart, false);
        }

        public MediaLine CreateLongMediaSubLine(float distanceToRayStart)
        {
            return CreateMediaSubLine(distanceToRayStart, true);
        }

        //Gibt ein Teilstück von der Gesamtlinie zurück. distanceToRayStart muss kleiner als die ShortRayLength sein wenn man eine korrekte PdfL von allen Segmenten will
        //wenn keepRemainingSegmentsAfterDistanceToRayStartPoint, dann werden die restlichen Segmente, welcher nach dem distanceToRayStart auch noch beibehalten
        private MediaLine CreateMediaSubLine(float distanceToRayStart, bool keepRemainingSegmentsAfterDistanceToRayStartPoint)
        {
            //Segment.RayMax errechnet sich über Distanzsampling und ShortRayLength über EndPoint-Start-Point-Distanz
            //Wegen unterschiedlicher Rechnung kommt es zu kleinen Abweichung so dass ich hier noch +1 für die Kontrolle rechne
            //Ich prüfe hier doch nicht, da DirectLightingOnEdge auch für LongRay-Segmente sampelt und dann selber die PdfL errechnet
            //if (distanceToRayStart > this.ShortRayLength + 1) throw new ArgumentException($"{nameof(distanceToRayStart)} have to be smaller then {nameof(ShortRayLength)}");

            int i = 0;
            List<VolumeSegment> newSegments = new List<VolumeSegment>();
            while (distanceToRayStart > this.Segments[i].RayMax)
            {
                newSegments.Add(this.Segments[i]);
                i++;
            }
            var pointOnT = MediaIntersectionPoint.CreateMediaPoint(this.Segments[i].PoinOnRayMin, this.Ray.Start + this.Ray.Direction * distanceToRayStart, MediaPointLocationType.MediaParticle);

            var segment = this.Segments[i];
            bool startPointIsOnParticleInMedium = segment == this.Segments.First() && this.StartPoint.Location == MediaPointLocationType.MediaParticle;
            bool endPointIsOnParticleInMedium = true;
            var pdf = segment.Media.DistanceSampler.GetSamplePdfFromRayMinToInfinity(this.Ray, segment.RayMin, segment.RayMax, distanceToRayStart, startPointIsOnParticleInMedium, endPointIsOnParticleInMedium);
            newSegments.Add(new VolumeSegment(this.Ray, segment.PoinOnRayMin, new RaySampleResult()
            {
                PdfL = pdf.PdfL,
                ReversePdfL = pdf.ReversePdfL,
                RayPosition = distanceToRayStart
            }, segment.RayMin, distanceToRayStart));

            //Füge noch restliche Segmente ein
            if (keepRemainingSegmentsAfterDistanceToRayStartPoint)
            {
                int shortRaySegmentCount = i + 1;
                newSegments.Add(new VolumeSegment(this.Ray, segment.PoinOnRayMin, new RaySampleResult()
                {
                    PdfL = 1,
                    ReversePdfL = 1,
                    RayPosition = segment.RayMax
                }, distanceToRayStart, segment.RayMax ));
                i++;
                while (i < this.Segments.Count)
                {
                    newSegments.Add(this.Segments[i]);
                    i++;
                }

                return MediaLine.CreateLongRayLine(this.Ray, this.StartPoint, pointOnT, newSegments, shortRaySegmentCount);
            }

            return MediaLine.CreateShortRayLine(this.Ray, this.StartPoint, pointOnT, newSegments);
        }
    }
}

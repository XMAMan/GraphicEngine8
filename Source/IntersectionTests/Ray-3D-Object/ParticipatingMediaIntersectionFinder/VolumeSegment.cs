using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media;

namespace IntersectionTests
{
    //Ist das Stück von ein Strahl zwischen zwei Surface/Media-Points. Es beginnt bei RayMin und läuft bis RayMax
    public class VolumeSegment
    {
        public float PdfL { get; private set; } //Mit dieser PdfL wurde das Segment gesampelt
        public float PdfLReverse { get; private set; }//Das ist die PdfL, wenn man in die Gegenrichtung gehen würde
        public Vector3D Attenuation { get; private set; }
        public Vector3D Emission { get; private set; }
        public Ray Ray { get; private set; }
        public float RayMin { get; set; }
        public float RayMax { get; set; }

        public IParticipatingMedia Media { get; private set; }
        public MediaIntersectionPoint PoinOnRayMin { get; private set; }

        public float SegmentLength
        {
            get
            {
                return this.RayMax - this.RayMin;
            }
        }

        public VolumeSegment(Ray ray, MediaIntersectionPoint poinOnRayMin, RaySampleResult sampleResult, float rayMin, float rayMax)
        {
            this.PoinOnRayMin = poinOnRayMin;
            this.Media = poinOnRayMin.CurrentMedium;
            this.PdfL = sampleResult.PdfL;
            this.PdfLReverse = sampleResult.ReversePdfL;
            this.Attenuation = this.Media.EvaluateAttenuation(ray, rayMin, rayMax);
            this.Emission = this.Media.EvaluateEmission(ray, rayMin, rayMax);
            this.Ray = ray;
            this.RayMin = rayMin;
            this.RayMax = rayMax;
        }

        public Vector3D EvaluateAttenuation(float distanceToRayStart)
        {
            if (distanceToRayStart > this.RayMax) return this.Attenuation;

            return Media.EvaluateAttenuation(this.Ray, this.RayMin, distanceToRayStart);
        }
    }
}

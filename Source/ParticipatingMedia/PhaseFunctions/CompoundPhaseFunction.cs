using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.PhaseFunctions
{
    //Wenn ein Medium zwei verschiedene Medium-Teilchen hat(z.B: Mie und Rayleigh) dann muss dessen Media-Klasse dieses Interface implemntieren,
    //damit man für ein Media-Punkt sagen kann, wie das Verhältniss der beiden Media-Dichten zueinander ist. Die CompoundPhaseFunction benötigt
    //diese Angabe, welche als Property mit am Gesamtmedium hängt. 
    interface ICompoundPhaseWeighter
    {
        float GetCompoundPhaseFunctionWeight(Vector3D mediaPoint); //Gibt das Gewicht vom p1 zurück. p2 wird dann mit 1-Gewicht gewichtet
    }

    class CompoundPhaseFunction : IPhaseFunction
    {
        private IPhaseFunction p1;
        private IPhaseFunction p2;
        private ICompoundPhaseWeighter weighter;

        public CompoundPhaseFunction(IPhaseFunction p1, IPhaseFunction p2, ICompoundPhaseWeighter weighter)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.weighter = weighter;
        }

        public PhaseSampleResult SampleDirection(Vector3D mediaPoint, Vector3D directionToPoint, IRandom rand)
        {
            if (rand.NextDouble() < this.weighter.GetCompoundPhaseFunctionWeight(mediaPoint))
                return this.p1.SampleDirection(mediaPoint, directionToPoint, rand);
            else
                return this.p2.SampleDirection(mediaPoint, directionToPoint, rand);
        }

        public PhaseFunctionResult GetBrdf(Vector3D directionToMediaPoint, Vector3D mediaPoint, Vector3D outDirection)
        {
            var r1 = this.p1.GetBrdf(directionToMediaPoint, mediaPoint, outDirection);
            var r2 = this.p2.GetBrdf(directionToMediaPoint, mediaPoint, outDirection);
            float f = this.weighter.GetCompoundPhaseFunctionWeight(mediaPoint);
            return new PhaseFunctionResult()
            {
                 Brdf = r1.Brdf * f + r2.Brdf * (1-f),
                 PdfW = r1.PdfW * f + r2.PdfW * (1 - f),
                 PdfWReverse = r1.PdfWReverse * f + r2.PdfWReverse * (1 - f),
            };
        }        
    }
}

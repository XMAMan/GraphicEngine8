using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.PhaseFunctions;
using System;

namespace ParticipatingMedia.Media
{
    //Die Dichte von dem Medium ist überall gleich (Homogen)
    //Man darf sich das Homogene Media so verstellen, dass es aus zwei Arten von Kugeln besteht: Scattering-Kugeln, welche das Licht reflektieren. Absorbationskugeln, welche das Licht auslöschen.
    //Die Dichte der Scattering-Kugeln wird über den ScatteringCoeffizent bestimmt. Die Dichte der Absorbationskugeln wird über den AbsorbationCoeffizent bestimmt
    class ParticipatingMediaHomogen : IParticipatingMedia
    {
        private readonly Vector3D AbsorbationCoeffizent;
        private readonly Vector3D EmissionCoeffizient; //Bei Feuer ist das > 0
        private readonly Vector3D ScatteringCoeffizent;

        private readonly bool hasScattering;
        private readonly Vector3D AttenuationCoeffizent;

        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }

        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }

        public ParticipatingMediaHomogen(int priority, float refractionIndex, DescriptionForHomogeneousMedia mediaDescription)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.AbsorbationCoeffizent = mediaDescription.AbsorbationCoeffizent;
            this.EmissionCoeffizient = mediaDescription.EmissionCoeffizient;
            this.ScatteringCoeffizent = mediaDescription.ScatteringCoeffizent;
            this.AttenuationCoeffizent = mediaDescription.AbsorbationCoeffizent + mediaDescription.ScatteringCoeffizent;

            //this.ContinuationProbability = mediaDescription.ScatteringCoeffizent.Max() / this.AttenuationCoeffizent.Max();

            this.hasScattering = mediaDescription.ScatteringCoeffizent.X > 0 || mediaDescription.ScatteringCoeffizent.Y > 0 || mediaDescription.ScatteringCoeffizent.Z > 0;

            if (mediaDescription.AnisotropyCoeffizient == 0)
                this.PhaseFunction = new IsotrophicPhaseFunction();
            else
                this.PhaseFunction = new AnisotropicPhaseFunction(mediaDescription.AnisotropyCoeffizient, mediaDescription.PhaseFunctionExtraFactor);
            
            this.DistanceSampler = new HomogenDistanceSampler(this.AttenuationCoeffizent, this.hasScattering);
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return this.hasScattering;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return this.hasScattering;
        }

        //Der Scattering-Faktor steht für die Dichte. Um so mehr er gegen 1 (oder sogar höher) geht, um so dichter ist das Material. Ist er 0, dann entspricht das der minimalen Dichte.
        //double distance = 10;
        //double attenuation1 = Math.Exp(-0.1 * distance); // 0.36787944117144233     So viel Licht kommt am Ende noch durch (Medium mit geringer Dichte=0.1)
        //double attenuation2 = Math.Exp(-0.9 * distance); // 0.00012340980408667956  (Medim mit hoher Dichte=0.9)
        //double pdf1 = attenuation1 * 0.1;                // 0.036787944117144235    Wahrscheinlichkeit, dass Distanz in wenig dichten Medium zurück gelegt wird
        //double pdf2 = attenuation2 * 0.9;                // 0.00011106882367801161  Wahrscheinlichkeit, dass Distanz in hoch-dichten Medium zurück gelegt wird


        //Gibt an, wie viel Prozent des Lichts durchgelassen wird (Maximaler Returnwert ist somit (1,1,1) = 100%)
        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            float distance = rayMax - rayMin;
            return new Vector3D((float)Math.Exp(-this.AttenuationCoeffizent.X * distance),
                              (float)Math.Exp(-this.AttenuationCoeffizent.Y * distance),
                              (float)Math.Exp(-this.AttenuationCoeffizent.Z * distance));
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            float distance = rayMax - rayMin;
            return this.EmissionCoeffizient * distance;
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            return this.ScatteringCoeffizent;
           
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return this.AbsorbationCoeffizent;
        }
    }
}

using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.PhaseFunctions;
using RaytracingRandom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticipatingMedia.Media
{
    //IMediaOnWaveLength = Wenn dieses Interface genutzt wird, dann wird ein Distanzsampler für inhomogene Medien genutzt
    //ICompoundPhaseWeighter = Wenn dieses Interface genutzt wird, dann besteht dieses Medium aus zwei verschiedenen Media-Teilchen
    //Quelle: https://www.scratchapixel.com/lessons/procedural-generation-virtual-worlds/simulating-sky
    public class ParticipatingMediaSky : IParticipatingMedia, IMediaOnWaveLength, ICompoundPhaseWeighter, IInhomogenMedia
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }
        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }
        public Vector3D MaxExtinctionCoeffizient { get; private set; }

        private DescriptionForSkyMedia mediaDescription;
        private SkyIntegrator skyIntegratorRayleigh;
        private SkyIntegrator skyIntegratorMie;

        private static Dictionary<string, ParticipatingMediaSky> skyCache = new Dictionary<string, ParticipatingMediaSky>();
        public static ParticipatingMediaSky CreateInstance(int priority, float refractionIndex, DescriptionForSkyMedia mediaDescription)
        {
            string key = mediaDescription.AtmosphereRadius.ToString();

            if (skyCache.ContainsKey(key) == false) skyCache.Add(key, new ParticipatingMediaSky(priority, refractionIndex, mediaDescription));
            return skyCache[key];
        }

        public ParticipatingMediaSky(int priority, float refractionIndex, DescriptionForSkyMedia mediaDescription)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            //this.PhaseFunction = new RayleighPhaseFunction();
            //this.PhaseFunction = new MiePhaseFunction(mediaDescription.MieAnisotrophieCoeffizient);
            this.PhaseFunction = new CompoundPhaseFunction(new RayleighPhaseFunction(), new MiePhaseFunction(mediaDescription.MieAnisotrophieCoeffizient), this);
            this.mediaDescription = mediaDescription;

            this.MaxExtinctionCoeffizient = mediaDescription.RayleighScatteringCoeffizientOnSeaLevel + mediaDescription.MieScatteringCoeffizientOnSeaLevel * 1.1f;

            this.skyIntegratorRayleigh = new SkyIntegrator(new LayerOfAirDescription()
            {
                EarthCenter = mediaDescription.CenterOfEarth,
                EarthRadius = mediaDescription.EarthRadius,
                AtmosphereRadius = mediaDescription.AtmosphereRadius,
                ScaleHeigh = mediaDescription.RayleighScaleHeight,
            });

            this.skyIntegratorMie = new SkyIntegrator(new LayerOfAirDescription()
            {
                EarthCenter = mediaDescription.CenterOfEarth,
                EarthRadius = mediaDescription.EarthRadius,
                AtmosphereRadius = mediaDescription.AtmosphereRadius,
                ScaleHeigh = mediaDescription.MieScaleHeight,
            });

            this.DistanceSampler = new WoodCockTrackingDistanceSamplerWithEqualSegmentSampling(this);
        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepthExpRay = this.skyIntegratorRayleigh.GetIntegralFromLine(ray, rayMin, rayMax);
            double opticalDepthExpMie = this.skyIntegratorMie.GetIntegralFromLine(ray, rayMin, rayMax);
            Vector3D opticalDepthRay = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel * (float)opticalDepthExpRay;
            Vector3D opticalDepthMie = this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * (float)opticalDepthExpMie * 1.1f; // 1.1 = Absorbation beträgt 10% vom Scattering
            Vector3D opticalDepth = opticalDepthRay + opticalDepthMie;
            //opticalDepth = opticalDepthMie;

            return new Vector3D(
                    (float)Math.Exp(-opticalDepth.X),
                    (float)Math.Exp(-opticalDepth.Y),
                    (float)Math.Exp(-opticalDepth.Z)
                );
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            float height = Math.Max(0, (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactor = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            return this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * expFactor * 0.1f;
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            float height = Math.Max(0, (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactorRay = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
            float expFactorMie = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            return this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel * expFactorRay + this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * expFactorMie;
            //return this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * expFactorMie;
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            float height = Math.Max(0, (point - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactorRay = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
            float expFactorMie = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            return (expFactorRay + expFactorMie) > 0; //Ganz knapp über die Erde ist der Scattering so klein, dass er kleiner als Float.Epsilon ist
            //return true;
        }
        public float ExtinctionCoeffizientOnWave(Vector3D position)
        {
            float height = Math.Max(0, (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactorRay = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
            float expFactorMie = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            return this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * expFactorRay + this.mediaDescription.MieScatteringCoeffizientOnSeaLevel.Z * expFactorMie * 1.1f;

        }

        public float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepthExpRay = this.skyIntegratorRayleigh.GetIntegralFromLine(ray, rayMin, rayMax);
            double opticalDepthExpMie = this.skyIntegratorMie.GetIntegralFromLine(ray, rayMin, rayMax);
            double opticalDepthRay = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * (float)opticalDepthExpRay;
            double opticalDepthMie = this.mediaDescription.MieScatteringCoeffizientOnSeaLevel.Z * (float)opticalDepthExpMie * 1.1f; // 1.1 = Absorbation beträgt 10% vom Scattering
            double opticalDepth = opticalDepthRay + opticalDepthMie;

            return (float)Math.Exp(-opticalDepth);
        }

        public float GetCompoundPhaseFunctionWeight(Vector3D mediaPoint)
        {
            float height = Math.Max(0, (mediaPoint - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactorRay = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
            float expFactorMie = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            float os1 = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * expFactorRay;
            float os2 = this.mediaDescription.MieScatteringCoeffizientOnSeaLevel.Z * expFactorMie;
            return os1 / (os1 + os2);

        }
    }

    public class ParticipatingMediaRayleighSky : IParticipatingMedia, IMediaOnWaveLength
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }
        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }
        public Vector3D MaxExtinctionCoeffizient { get; private set; }

        private DescriptionForSkyMedia mediaDescription;
        private SkyIntegrator skyIntegrator;

        public ParticipatingMediaRayleighSky(int priority, float refractionIndex, DescriptionForSkyMedia mediaDescription)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.PhaseFunction = new RayleighPhaseFunction();
            this.mediaDescription = mediaDescription;
            this.MaxExtinctionCoeffizient = mediaDescription.RayleighScatteringCoeffizientOnSeaLevel;

            this.skyIntegrator = new SkyIntegrator(new LayerOfAirDescription()
            {
                EarthCenter = mediaDescription.CenterOfEarth,
                EarthRadius = mediaDescription.EarthRadius,
                AtmosphereRadius = mediaDescription.AtmosphereRadius,
                ScaleHeigh = mediaDescription.RayleighScaleHeight,
            });

            this.DistanceSampler = new WoodCockTrackingDistanceSampler(this);
        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepthExp = this.skyIntegrator.GetIntegralFromLine(ray, rayMin, rayMax);
            Vector3D opticalDepth = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel * (float)opticalDepthExp;

            return new Vector3D(
                    (float)Math.Exp(-opticalDepth.X),
                    (float)Math.Exp(-opticalDepth.Y),
                    (float)Math.Exp(-opticalDepth.Z)
                );
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return new Vector3D(0, 0, 0); //Luft ist blau und somit absorbiert es nah null
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            float height = Math.Max(0, (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactor = (float)Math.Exp(-height / this.mediaDescription.RayleighScaleHeight);
            return this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel * expFactor;
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return true;
        }
        public float ExtinctionCoeffizientOnWave(Vector3D position)
        {
            return GetScatteringCoeffizient(position).Z;
        }

        public float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepth = this.mediaDescription.RayleighScatteringCoeffizientOnSeaLevel.Z * this.skyIntegrator.GetIntegralFromLine(ray, rayMin, rayMax);
            return (float)Math.Exp(-opticalDepth);
        }
    }

    public class ParticipatingMediaMieSky : IParticipatingMedia, IMediaOnWaveLength
    {
        public int Priority { get; private set; }
        public float RefractionIndex { get; private set; }
        public IPhaseFunction PhaseFunction { get; private set; }
        public IDistanceSampler DistanceSampler { get; private set; }
        public Vector3D MaxExtinctionCoeffizient { get; private set; }

        private DescriptionForSkyMedia mediaDescription;
        private SkyIntegrator skyIntegrator;

        public ParticipatingMediaMieSky(int priority, float refractionIndex, DescriptionForSkyMedia mediaDescription)
        {
            this.Priority = priority;
            this.RefractionIndex = refractionIndex;
            this.PhaseFunction = new MiePhaseFunction(mediaDescription.MieAnisotrophieCoeffizient);
            this.mediaDescription = mediaDescription;
            this.MaxExtinctionCoeffizient = mediaDescription.MieScatteringCoeffizientOnSeaLevel;
            this.skyIntegrator = new SkyIntegrator(new LayerOfAirDescription()
            {
                EarthCenter = mediaDescription.CenterOfEarth,
                EarthRadius = mediaDescription.EarthRadius,
                AtmosphereRadius = mediaDescription.AtmosphereRadius,
                ScaleHeigh = mediaDescription.MieScaleHeight,
            });

            this.DistanceSampler = new WoodCockTrackingDistanceSampler(this);
            //this.DistanceSampler = new RayMarchingDistanceSampler(this);
        }

        public Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepthExp = this.skyIntegrator.GetIntegralFromLine(ray, rayMin, rayMax);
            Vector3D opticalDepth = this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * (float)opticalDepthExp;

            return new Vector3D(
                    (float)Math.Exp(-opticalDepth.X),
                    (float)Math.Exp(-opticalDepth.Y),
                    (float)Math.Exp(-opticalDepth.Z)
                );
        }

        public Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax)
        {
            return new Vector3D(0, 0, 0);
        }

        public Vector3D GetAbsorbationCoeffizient(Vector3D position)
        {
            return GetScatteringCoeffizient(position) * 0.1f;
        }

        public Vector3D GetScatteringCoeffizient(Vector3D position)
        {
            float height = Math.Max(0, (position - this.mediaDescription.CenterOfEarth).Length() - this.mediaDescription.EarthRadius);
            float expFactor = (float)Math.Exp(-height / this.mediaDescription.MieScaleHeight);
            return this.mediaDescription.MieScatteringCoeffizientOnSeaLevel * expFactor;
        }

        public bool HasScatteringSomeWhereInMedium()
        {
            return true;
        }

        public bool HasScatteringOnPoint(Vector3D point)
        {
            return true;
        }

        public float ExtinctionCoeffizientOnWave(Vector3D position)
        {
            return GetScatteringCoeffizient(position).Z *1.1f;
        }

        public float EvaluateAttenuationOnWave(Ray ray, float rayMin, float rayMax)
        {
            double opticalDepth = this.mediaDescription.MieScatteringCoeffizientOnSeaLevel.Z * this.skyIntegrator.GetIntegralFromLine(ray, rayMin, rayMax);
            return (float)Math.Exp(-opticalDepth);
        }
    }
    
    public class LayerOfAirDescription
    {
        public Vector3D EarthCenter;
        public double EarthRadius;      //Gemessen vom EarthCenter
        public double AtmosphereRadius; //Gemessen vom EarthCenter
        public double ScaleHeigh;
    }

    //Gegeben sind zwei 3D-Punkte innerhalb der Atmospähre. Diese Klasse berechnet das Integral int from {t=0} to {(p2-p1).Betrag} exp(-h(t)/scaleHeigh) h(t)=Höhe eines 3D-Punktes zur Erdoberfläche
    public class SkyIntegrator
    {
        class CdsTableOverTheta
        {
            private PdfWithTableSampler[] tableCdfs;

            public CdsTableOverTheta(LayerOfAirDescription layerOfAir, double heighOverGround, int thetaStepCount, int tStepCount)
            {
                this.tableCdfs = new PdfWithTableSampler[thetaStepCount];

                for (int i=0;i<thetaStepCount;i++)
                {
                    double theta = i / (double)(thetaStepCount - 1) * Math.PI; //Gehe von 0 bis PI (0 bis 180 Grad)
                    Vector2D direction = new Vector2D((float)Math.Sin(theta), (float)Math.Cos(theta));

                    //Erzeuge Linie von Punkt, der heighOverGround über der Erde liegt und in Richtung phi bis zur Atmosphäre reicht (p1 bis p2)                    
                    Vector2D p1 = new Vector2D(0, (float)(layerOfAir.EarthRadius + heighOverGround));
                    Vector2D p2 = IntersectionHelper2D.IntersectionPointRayCircle(p1, direction, new Vector2D(0, 0), (float)layerOfAir.AtmosphereRadius);
                    double tMin = 0;
                    if (p2 != null)
                    {
                        double tMax = (p2 - p1).Length();

                        this.tableCdfs[i] = PdfWithTableSampler.CreateFromUnnormalisizedFunction((t) =>
                        {
                            //Partikeldichte an Punkt t
                            Vector2D pointOnT = p1 + (float)t * direction;
                            double heighOverGroundFromPointT = Math.Max(0, pointOnT.Length() - layerOfAir.EarthRadius);
                            return Math.Exp(-heighOverGroundFromPointT / layerOfAir.ScaleHeigh);
                        }, tMin, tMax, tStepCount);
                    }                                    
                }
            }

            public double GetIntegralFromRay(double theta, double length)
            {
                if (theta == Math.PI) return this.tableCdfs.Last().IntegralFromMinXValueToX(length);

                double indexD = (theta / Math.PI) * (this.tableCdfs.Length - 1);
                int index = Math.Min((int)indexD, this.tableCdfs.Length - 2);
                double f = indexD - index;

                if (this.tableCdfs[index] == null) return 0;

                return (1 - f) * this.tableCdfs[index].IntegralFromMinXValueToX(length) + f * (this.tableCdfs[index + 1].IntegralFromMinXValueToX(length));
            }
        }

        private LayerOfAirDescription layerOfAir;
        private NonLinearFunctionScanner heighFunction; //Zum nichtlinearen Abtasten der Höhenwerte
        private CdsTableOverTheta[] tFunctions; //Lauter Strahlen, welche in Richtung Theta von heighFunction[index] bis zur Atmosphäre laufen

        public SkyIntegrator(LayerOfAirDescription layerOfAir)
            :this(layerOfAir, 4, 8192, 64)
        { }

        public SkyIntegrator(LayerOfAirDescription layerOfAir, int hStepCount, int thetaStepCount, int tStepCount)
        {
            this.layerOfAir = layerOfAir;
            this.heighFunction = new NonLinearFunctionScanner((h) => { return Math.Exp(-h / layerOfAir.ScaleHeigh); }, 0, layerOfAir.AtmosphereRadius - layerOfAir.EarthRadius, hStepCount);
            this.tFunctions = new CdsTableOverTheta[hStepCount];
            for (int hIndex = 0;hIndex < hStepCount;hIndex++)
            {
                this.tFunctions[hIndex] = new CdsTableOverTheta(layerOfAir, this.heighFunction.XValues[hIndex], thetaStepCount, tStepCount);
            }
        }

        //private Random rand = new Random();
        public double GetIntegralFromLine(Ray ray, float rayMin, float rayMax)
        {
            return GetIntegralFromLine(ray.Start + ray.Direction * rayMin, ray.Start + ray.Direction * rayMax);
        }


        public double GetIntegralFromLine(Vector3D p1, Vector3D p2)
        {           
            double h1 = (p1 - this.layerOfAir.EarthCenter).SquareLength();
            double h2 = (p2 - this.layerOfAir.EarthCenter).SquareLength();

            if (h1 < h2)
            {
                return IntegralFromP1ToP2(p1, p2);
            }
            else
                return IntegralFromP1ToP2(p2, p1);
        }

        //Berechnet das Integral int from {s=0} to {(p2-p1).Betrag} exp(-h(s)/scaleHeigh)
        private double IntegralFromP1ToP2(Vector3D p1, Vector3D p2)
        {
            double length = (p2 - p1).Length();
            if (length == 0) return 0;
            double theta = Math.Acos(Math.Max(-1, Math.Min(1, Vector3D.Normalize(p1 - this.layerOfAir.EarthCenter) * Vector3D.Normalize(p2 - p1))));
            var scanHeigh = this.heighFunction.GetScanPosition(HeighFrom3DPoint(p1));
            return this.tFunctions[scanHeigh.Index].GetIntegralFromRay(theta, length) * (1 - scanHeigh.F) + this.tFunctions[scanHeigh.Index + 1].GetIntegralFromRay(theta, length) * scanHeigh.F;
        }

        

        private double HeighFrom3DPoint(Vector3D p)
        {
            return Math.Max(0, (p - this.layerOfAir.EarthCenter).Length() - this.layerOfAir.EarthRadius);
        }
    }
}

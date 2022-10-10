using System.Collections.Generic;
using System.Linq;
using RayObjects;
using GraphicMinimal;
using GraphicGlobal;

namespace RaytracingLightSource
{
    class FlatSurfaceListSamplingUniform : IUniformRandomSurfacePointCreator
    {
        private List<float> probabilities = null;
        private readonly List<IFlatRandomPointCreator> flats;

        public FlatSurfaceListSamplingUniform(IEnumerable<IFlatRandomPointCreator> flats)
        {
            this.flats = flats.ToList();
            this.SurfaceArea = flats.Sum(x => x.SurfaceArea);
            UpdateProbabilities();
        }

        private void UpdateProbabilities()
        {
            if (probabilities != null) return;

            probabilities = new List<float>();

            float cumulativeSurfaceArea= 0;
            for (int i = 0; i < flats.Count; i++)
            {
                cumulativeSurfaceArea += flats[i].SurfaceArea;
                probabilities.Add(cumulativeSurfaceArea / this.SurfaceArea);
            }
        }

        private IFlatRandomPointCreator GetRandomFlat(IRandom rand)
        {
            float randNumber = (float)rand.NextDouble();
            for (int i = 0; i < this.probabilities.Count; i++)
            {
                if (this.probabilities[i] >= randNumber)
                {
                    return flats[i];
                }
            }

            return flats.Last();
        }

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            var flat = GetRandomFlat(rand);
            var point = flat.GetRandomPointOnSurface(rand);
            return new SurfacePoint(point.Position, point.Normal, point.Color, flat, flat.SurfaceArea / this.SurfaceArea * point.PdfA);
        }

        public float SurfaceArea { get; private set; }
    }

    public class FlatSurfaceListSamplingNonUniform
    {
        private readonly List<FlatData> flats = new List<FlatData>();

        public FlatSurfaceListSamplingNonUniform(IEnumerable<IFlatRandomPointCreator> flats, Vector3D hitpoint)
        {
            float solidAngleSum = 0;
            foreach (var flat in flats)
            {
                if (flat.IsPointAbovePlane(hitpoint))
                {
                    float cosAtLight = (Vector3D.Normalize(hitpoint - flat.CenterOfGravity) * flat.Normal);
                    if (cosAtLight > 0)
                    {
                        float solidAngle = flat.SurfaceArea * cosAtLight;
                        solidAngleSum += solidAngle;
                        this.flats.Add(new FlatData()
                        {
                            Flat = flat,
                            SolidAngleRunningSum = solidAngleSum,
                            SolidAngle = solidAngle
                        });
                    }
                }
            }

            this.SurfaceArea = solidAngleSum;
        }

        private FlatData GetRandomFlat(IRandom rand)
        {
            float randNumber = (float)rand.NextDouble() * this.SurfaceArea;
            for (int i = 0; i < this.flats.Count; i++)
            {
                if (this.flats[i].SolidAngleRunningSum >= randNumber)
                {
                    return this.flats[i];
                }
            }

            return this.flats.Last();
        }

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            if (this.flats.Any() == false) return null;
            var triBum = GetRandomFlat(rand);
            var randomPoint = triBum.Flat.GetRandomPointOnSurface(rand);
            randomPoint.PdfA *= triBum.SolidAngle / this.SurfaceArea; //Triangle-Selection-Pdf * (1 / TriangleSurfaceArea)
            return randomPoint;
        }

        public float SurfaceArea { get; private set; }

        public float PdfA(IFlatRandomPointCreator intersectedFlatObject)
        {
            var flat = intersectedFlatObject;
            float flatSurfaceSelectionPdf = FlatSurfaceSelectionPdf(flat);
            return 1.0f / flat.SurfaceArea * flatSurfaceSelectionPdf;
        }

        private float FlatSurfaceSelectionPdf(IFlatRandomPointCreator flat)
        {
            var triBum = this.flats.FirstOrDefault(x => x.Flat == flat);
            if (triBum == null) return 0; //Wenn keine Fläche zum Hitpoint zeigt, dann ist DirectLighint changenlos
            return triBum.SolidAngle / this.SurfaceArea;
        }

        class FlatData
        {
            public IFlatRandomPointCreator Flat;
            public float SolidAngleRunningSum;
            public float SolidAngle;
        }
    }
}

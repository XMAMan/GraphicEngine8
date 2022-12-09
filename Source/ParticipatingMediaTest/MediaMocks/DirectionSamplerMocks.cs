using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParticipatingMediaTest.MediaMocks
{
    public class DirectionSamplingMockData
    {
        public List<Vector3D> ReturnValuesForDirectionSampling;
        public List<Vector3D> ExpectedPointLocationForDirectionSampling;
    }

    public class PhaseFunctionMock : IPhaseFunctionSampler
    {
        private int index = -1;
        private readonly List<Vector3D> returnValuesForBrdfDirectionSampling;
        private readonly List<Vector3D> expectedSurfacePointsForDirectionSampling;
        private readonly PhaseFunction phaseFunctionNoMock;
        public PhaseFunctionMock(DirectionSamplingMockData data)
        {
            if (data != null)
            {
                this.returnValuesForBrdfDirectionSampling = data.ReturnValuesForDirectionSampling;
                this.expectedSurfacePointsForDirectionSampling = data.ExpectedPointLocationForDirectionSampling;
            }
            this.phaseFunctionNoMock = new PhaseFunction();
        }

        public BrdfSampleEvent SampleDirection(MediaIntersectionPoint mediaPoint, Vector3D directionToMediaPoint, IRandom rand)
        {
            this.index++;
            if (this.index >= this.returnValuesForBrdfDirectionSampling.Count) throw new Exception("Mehr Returnvalues habe ich nicht");
            if (mediaPoint.Position != this.expectedSurfacePointsForDirectionSampling[index]) throw new Exception("Erwarteter SurfacePunkt " + this.expectedSurfacePointsForDirectionSampling[index] + " stimmt nicht mit übergebenen Punkt " + mediaPoint.Position + " überrein");
            if (this.returnValuesForBrdfDirectionSampling[this.index] == null) return null;
            return new BrdfSampleEvent()
            {
                Brdf = new Vector3D(1, 1, 1),
                ExcludedObject = null,
                IsSpecualarReflected = false,
                PdfW = 1,
                PdfWReverse = 1,
                Ray = new Ray(mediaPoint.Position, this.returnValuesForBrdfDirectionSampling[this.index]),
                RayWasRefracted = false
            };
        }

        public BrdfEvaluateResult EvaluateBsdf(Vector3D directionToBrdfPoint, MediaIntersectionPoint brdfPoint, Vector3D outDirection)
        {
            return this.phaseFunctionNoMock.EvaluateBsdf(directionToBrdfPoint, brdfPoint, outDirection);
        }
    }

    public class BrdfMockData
    {
        public Vector3D ExpectedLocation;
        public Vector3D ReturnValueForDirectionSampling;
        public float ExpectedRefractionIndexRaysComesFrom = 1;
        public float ExpectedRefrationIndexRaysGoesInto = float.NaN;
    }

    public class DirectionSamplerMocks : IBrdfSampler
    {
        private int index = -1;
        private readonly List<BrdfMockData> data;

        public DirectionSamplerMocks(List<BrdfMockData> data)
        {
            this.data = data;
        }
        public BrdfSampleEvent CreateDirection(BrdfPoint brdfPoint, IRandom rand)
        {
            this.index++;
            if (this.index >= this.data.Count) throw new Exception("Mehr Returnvalues habe ich nicht");
            if (brdfPoint.SurfacePoint.Position != this.data[index].ExpectedLocation) throw new Exception("Erwarteter SurfacePunkt " + this.data[index].ExpectedLocation + " stimmt nicht mit übergebenen Punkt " + brdfPoint.SurfacePoint.Position + " überrein");
            if (float.IsNaN(this.data[index].ExpectedRefractionIndexRaysComesFrom) == false && brdfPoint.RefractionIndexFromRayComesFrom != this.data[index].ExpectedRefractionIndexRaysComesFrom) throw new Exception("Erwarteter RefractionIndexRayComesFrom " + this.data[index].ExpectedRefractionIndexRaysComesFrom + " stimmt nicht mit übergebenen Wert " + brdfPoint.RefractionIndexFromRayComesFrom + " überrein");
            if (float.IsNaN(this.data[index].ExpectedRefrationIndexRaysGoesInto) == false && brdfPoint.RefractionIndexOtherSide != this.data[index].ExpectedRefrationIndexRaysGoesInto) throw new Exception("Erwarteter RefractionIndexRayGoesInto " + this.data[index].ExpectedRefrationIndexRaysGoesInto + " stimmt nicht mit übergebenen Wert " + brdfPoint.RefractionIndexOtherSide + " überrein");
            if (this.data[this.index].ReturnValueForDirectionSampling == null) return null;

            bool rayWasRefracted = brdfPoint.DirectionToThisPoint * this.data[this.index].ReturnValueForDirectionSampling > 0;

            return new BrdfSampleEvent()
            {
                Brdf = new Vector3D(1,1,1),
                ExcludedObject = brdfPoint.SurfacePoint.IntersectedObject,
                IsSpecualarReflected = brdfPoint.IsOnlySpecular,
                PdfW = 1,
                PdfWReverse = 1,
                Ray = new Ray(brdfPoint.SurfacePoint.Position, this.data[this.index].ReturnValueForDirectionSampling),
                RayWasRefracted = rayWasRefracted
            };
        }
    }
}

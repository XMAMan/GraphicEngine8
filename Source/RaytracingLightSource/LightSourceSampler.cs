using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using RayObjects;
using IntersectionTests;
using RaytracingLightSource.RayLightSource;
using GraphicGlobal;
using RayObjects.RayObjects;

namespace RaytracingLightSource
{
    //Enthält alle Lichtquellen der Szene
    public class LightSourceSampler : IIntersectableEnvironmentLight
    {
        private readonly Dictionary<IIntersectableRayDrawingObject, SingleLightSource> lightSources;

        public bool ContainsEnvironmentLight { get => this.environmentLight != null; }
        private readonly IEnvironmentLightSource environmentLight = null;
        public IntersectionPoint GetIntersectionPointWithEnvironmentLight(Ray ray)
        {
            return this.environmentLight.GetIntersectionPoint(ray);
        }

        public LightSourceSampler(ConstruktorDataForLightSourceSampler lightCreationData)
        {
            this.lightSources = GetLightSourceDictionary(CreateRayLightSourceList(lightCreationData), lightCreationData.LightPickStepSize);

            this.environmentLight = this.lightSources.Select(x => x.Value.RayLightSource).FirstOrDefault(x => x is IEnvironmentLightSource) as IEnvironmentLightSource;
        }
        
        //Legt für jede Lichtquelle die lightPickProb fest
        private Dictionary<IIntersectableRayDrawingObject, SingleLightSource> GetLightSourceDictionary(List<IRayLightSource> lightSources, int lightPickStepSize)
        {
            if (lightSources.Count == 0) return new Dictionary<IIntersectableRayDrawingObject, SingleLightSource>();

            if (lightPickStepSize == 0)
            {
                //lightPickProb laut Emission
                return GetLightSourceDictionaryFromEmissionOnly(lightSources);
            }else
            {
                //lightPickProb laut Emission aber quantisiert um kleine Lampen keine zu kleine pickProb zuzuweisen
                return GetLightSourceDictionaryWithPickSteps(lightSources, lightPickStepSize);
            }          
        }

        private Dictionary<IIntersectableRayDrawingObject, SingleLightSource> GetLightSourceDictionaryWithPickSteps(List<IRayLightSource> lightSources, int lightPickStepSize)
        {
            //Erster Ansatz, um das Sonne-Tischlampe-Problem zu lösen: Steppisieren(Chunkbildung) der Emission
            //Schritt 1: Bringe alle Emission-Werte in ein Integerbereich von 0 bis 4 damit ein kleine schwache Lichtquelle, welche 
            //aber ganz nah zur Kamera ist trotzdem minimal nur ein viertel vom Gewicht von der hellsten Lampe bekommt
            int steps = lightPickStepSize; //Um so mehr diese Zahl gegen 1 geht, um so geringer wird das Rauschen, was aufgrund des ungleichen 
                                           //Gewichtes entsteht (in den Bildbereich, wo die kleine Lampe leuchet) aber im Bildbereich, wo hauptsächlich 
                                           //die große Helle Lampe leuchtet, wird das Rauschen schlimmer
            float maxEmission = lightSources.Max(x => x.Emission);
            var lightList = lightSources.Select(x => new SingleLightSource(x, Math.Min((int)(x.Emission / maxEmission * steps) + 1, steps), 0)).ToList();

            float emissionSum = lightList.Sum(x => x.LightPickProb);

            List<float> sum = new List<float>();
            float runningSum = 0;
            foreach (var l in lightList)
            {
                float pickProb = l.LightPickProb / emissionSum;
                runningSum += pickProb;
                sum.Add(runningSum);
            }

            Dictionary<IIntersectableRayDrawingObject, SingleLightSource> dic = new Dictionary<IIntersectableRayDrawingObject, SingleLightSource>();
            for (int i = 0; i < lightList.Count; i++)
            {
                var singleSources = new SingleLightSource(lightList[i].RayLightSource, lightList[i].LightPickProb / emissionSum, sum[i]);
                dic.Add(lightSources[i].RayDrawingObject, singleSources);
            }

            return dic;
        }

        //Alte Weg: Nur über Emission die LightPickProb bestimmen
        private Dictionary<IIntersectableRayDrawingObject, SingleLightSource> GetLightSourceDictionaryFromEmissionOnly(List<IRayLightSource> lightSources)
        {
            float emissionSum = lightSources.Sum(x => x.Emission);

            List<float> sum = new List<float>();
            float runningSum = 0;
            foreach (var l in lightSources)
            {
                float pickProb = l.Emission / emissionSum;
                runningSum += pickProb;
                sum.Add(runningSum);
            }

            Dictionary<IIntersectableRayDrawingObject, SingleLightSource> dic = new Dictionary<IIntersectableRayDrawingObject, SingleLightSource>();
            for (int i = 0; i < lightSources.Count; i++)
            {
                var singleSources = new SingleLightSource(lightSources[i], lightSources[i].Emission / emissionSum, sum[i]);
                dic.Add(lightSources[i].RayDrawingObject, singleSources);
            }

            return dic;
        }

        private List<IRayLightSource> CreateRayLightSourceList(ConstruktorDataForLightSourceSampler lightCreationData)
        {
            List<IRayLightSource> lightSources = new List<IRayLightSource>();

            foreach (var groupBy in lightCreationData.LightDrawingObjects.GroupBy(x => x.RayHeigh))
            {
                lightSources.Add(CreateLightSource(groupBy.Key.Propertys.RaytracingLightSource, groupBy.ToList(), lightCreationData));
            }

            return lightSources;
        }

        private IRayLightSource CreateLightSource(ILightSourceDescription lightsourceData, List<IRayObject> rayObjects, ConstruktorDataForLightSourceSampler lightCreationData)
        {
            if (lightsourceData is DiffuseSurfaceLightDescription)
            {
                if (rayObjects.Count() == 1 && rayObjects.First() is RayMotionObject)
                {
                    return new SurfaceWithMotion(rayObjects.First() as RayMotionObject);
                }
                else
                {
                    return new SurfaceDiffuse(rayObjects.Cast<IFlatRandomPointCreator>());
                }
            }

            if (lightsourceData is ImportanceSurfaceLightDescription)
            {
                return new ImportanceSurfaceLight(rayObjects.Cast<IUVMapable>().ToList(), lightCreationData, lightsourceData as ImportanceSurfaceLightDescription);
            }

            if (lightsourceData is ImportanceSurfaceWithSpotLightDescription)
            {
                return new ImportanceSurfaceWithSpot(rayObjects.Cast<IUVMapable>().ToList(), lightCreationData, lightsourceData as ImportanceSurfaceWithSpotLightDescription);
            }

            if (lightsourceData is DiffuseSphereLightDescription)
            {
                if (rayObjects.Count() == 1 && rayObjects.First() is RaySphere)
                {
                    return new SphereDiffuse(rayObjects.First() as RaySphere);
                }
                else
                {
                    return new SphereDiffuse(rayObjects.Cast<IFlatRandomPointCreator>());
                }
            }

            if (lightsourceData is SphereWithSpotLightDescription)
            {
                if (rayObjects.Count() == 1 && rayObjects.First() is RaySphere)
                {
                    return new SphereWithSpot(rayObjects.First() as RaySphere);
                }
                else
                {
                    return new SphereWithSpot(rayObjects.Cast<RayTriangle>());
                }
            }

            if (lightsourceData is SurfaceWithSpotLightDescription)
            {
                return new SurfaceWithSpot(rayObjects.Cast<RayTriangle>());
            }

            if (lightsourceData is FarAwayDirectionLightDescription)
            {
                return new FarAwayDiretionLightSource(rayObjects.Cast<RayTriangle>(), lightCreationData.IntersectionFinder, lightCreationData.MediaIntersectionFinder);
            }

            if (lightsourceData is EnvironmentLightDescription)
            {
                if (rayObjects.First().RayHeigh.Propertys.Color.Type == ColorSource.ColorString)
                    return new EnvironmentLightSourceWithEqualSampling(rayObjects.First(), lightCreationData.IntersectionFinder, lightCreationData.MediaIntersectionFinder);                
                else
                    return new EnvironmentLightSourceWithImageImportanceSampling(rayObjects.First(), lightCreationData.IntersectionFinder, lightCreationData.MediaIntersectionFinder, lightCreationData.RayCamera.Up);
            }

            throw new Exception("Can not create lightsource from " + rayObjects[0].RayHeigh.Propertys.Name);
        }

        private float GetLightPickProb(IIntersectableRayDrawingObject light)
        {
            return this.lightSources[light].LightPickProb;
        }

        private IRayLightSource GetRandomLightSource(IRandom rand)
        {
            float cmf = (float)rand.NextDouble();
            foreach (var keyValue in this.lightSources)
            {
                if (keyValue.Value.RunningLightPickProb >= cmf) return keyValue.Value.RayLightSource;
            }

            return lightSources.Values.Last().RayLightSource;
        }

        //############################################# 3 Möglichkeiten fürs Licht: Lightsourcesampling, BrdfSampling, Lighttracing
        public float GetDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.lightSources[pointOnLight.IntersectedRayHeigh].RayLightSource.GetDirectLightingPdfA(eyePoint, pointOnLight, pathCreationTime) * GetLightPickProb(pointOnLight.IntersectedRayHeigh);
        }

        public float GetMultipleDirectLightingPdfA(Vector3D eyePoint, IntersectionPoint pointOnLight, float pathCreationTime)
        {
            return this.lightSources[pointOnLight.IntersectedRayHeigh].RayLightSource.GetMultipleDirectLightingPdfA(eyePoint, pointOnLight, pathCreationTime);
        }

        public List<DirectLightingSampleResult> GetRandomPointOnLightList(Vector3D eyePoint, IRandom rand)
        {
            return this.lightSources.Values.SelectMany(x => x.RayLightSource.GetRandomPointOnLightList(eyePoint, rand)).ToList();
        }

        public float GetEmissionForEyePathHitLightSourceDirectly(IntersectionPoint pointOnLight, Vector3D eyePoint, Vector3D directionFromEyeToLightPoint)
        {
            return this.lightSources[pointOnLight.IntersectedRayHeigh].RayLightSource.GetEmissionForEyePathHitLightSourceDirectly(pointOnLight, eyePoint, directionFromEyeToLightPoint);
        }

        //Wahrscheinlichkeit, genau an diesen pointOnLight-Stelle per RandomPointOnLightSource-Sampling ein Punkt zu erzeugen
        public float PdfAFromRandomPointOnLightSourceSampling(IntersectionPoint pointOnLight)
        {
            return GetLightPickProb(pointOnLight.IntersectedRayHeigh) * this.lightSources[pointOnLight.IntersectedRayHeigh].RayLightSource.PdfAFromRandomPointOnLightSourceSampling(pointOnLight);
        }

        public float PdfWFromLightDirectionSampling(IntersectionPoint pointOnLight, Vector3D direction)
        {
            return this.lightSources[pointOnLight.IntersectedRayHeigh].RayLightSource.GetPdfWFromLightDirectionSampling(pointOnLight, direction);
        }

        public DirectLightingSampleResult GetRandomPointOnLight(Vector3D eyePoint, IRandom rand)
        {
            var light = GetRandomLightSource(rand);
            var direction = light.GetRandomPointOnLight(eyePoint, rand);
            if (direction == null) return null;
            direction.PdfA *= GetLightPickProb(light.RayDrawingObject);
            return direction;
        }

        public SurfaceLightPointForLightPathCreation GetRandomPointForLightPathCreation(IRandom rand)
        {
            var light = GetRandomLightSource(rand);
            var point = light.GetRandomPointForLightPathCreation(rand);
            if (point != null) point.PdfA *= GetLightPickProb(light.RayDrawingObject);
            return point;
        }
    }

    class SingleLightSource
    {
        public IRayLightSource RayLightSource { get; private set; }
        public float LightPickProb { get; private set; }
        public float RunningLightPickProb { get; private set; }

        public SingleLightSource(IRayLightSource rayLightSource, float lightPickProb, float runningLightPickProb)
        {
            this.RayLightSource = rayLightSource;
            this.LightPickProb = lightPickProb;
            this.RunningLightPickProb = runningLightPickProb;
        }
    }
}

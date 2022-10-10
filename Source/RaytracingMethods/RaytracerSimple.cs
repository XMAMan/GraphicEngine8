using System;
using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using RaytracingColorEstimator;
using FullPathGenerator;
using IntersectionTests;
using GraphicGlobal;
using SubpathGenerator;
using RaytracingBrdf;
using RaytracingBrdf.SampleAndRequest;

namespace RaytracingMethods
{
    public class RaytracerSimple : IPixelEstimator
    {
        private PixelRadianceCalculator pixelRadianceCalculator;
        private List<RasterLightSource> lightSources;
        private RayVisibleTester visibleTester;
        private IntersectionFinder intersectionFinderForStencilShadows; //Enthält nur die Objekte, welche ein Stencilschatten werfen
        private Vector3D specularReflectDir;

        public bool CreatesLigthPaths { get; } = false;

        public void BuildUp(RaytracingFrame3DData data)
        {
            var cam = data.GlobalObjektPropertys.Camera;
            Matrix4x4 eyeToWorld = Matrix4x4.Transpose(Matrix4x4.LookAt(cam.Position, cam.Forward, cam.Up));
            this.specularReflectDir = Vector3D.Normalize(Matrix4x4.MultDirection(eyeToWorld, new Vector3D(0, 0, 1)));

            this.pixelRadianceCalculator = PixelRadianceCreationHelper.CreatePixelRadianceCalculator(data,
                new SubPathSettings()
                {
                    EyePathType = PathSamplingType.NoMedia,
                    LightPathType = PathSamplingType.None
                },
                new FullPathSettings()
                {
                });

            this.visibleTester = new RayVisibleTester(this.pixelRadianceCalculator.IntersectionFinder, null);
            this.intersectionFinderForStencilShadows = new IntersectionFinder(this.pixelRadianceCalculator
                .GetIIntersecableObjectList()
                .Where(x =>
                    (x.RayHeigh.Propertys as ObjectPropertys).HasStencilShadow &&
                    (x.RayHeigh.Propertys as ObjectPropertys).RasterizerLightSource == null
                ).ToList(), null);

            //Die einzelnen Dreicke von den Rasterlichtquellen
            var rayobjectRasterLightSources = this.pixelRadianceCalculator.GetIIntersecableObjectList()
                .Where(x => (x.RayHeigh.Propertys as ObjectPropertys).RasterizerLightSource != null)
                .ToList();

            this.lightSources = data.DrawingObjects
                .Where(x => x.DrawingProps.RasterizerLightSource != null)
                .Select(x => new RasterLightSource(
                    rayobjectRasterLightSources.First(y => (y.RayHeigh.Propertys as ObjectPropertys).RasterizerLightSource == x.DrawingProps.RasterizerLightSource).RayHeigh,
                    x.DrawingProps.Position //Der Rasterizer nimmt die Positions-Property
                    ))
                .ToList();
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            Vector3D color = GetPixelColor(x, y, rand);
            return new FullPathSampleResult() { RadianceFromRequestetPixel = color != null ? GetPixelColor(x,y,rand) : new Vector3D(0,0,0), MainPixelHitsBackground = color == null };
        }

        private Vector3D GetPixelColor(int x, int y, IRandom rand)
        {
            //return this.pixelRadianceCalculator.SampleSingleEyePath(x, y, rand).RadianceFromRequestetPixel;

            IntersectionPoint point = this.pixelRadianceCalculator.GetFirstEyePoint(x, y);
            if (point == null) return null;

            //return point.Color;
            //return new Vector3D(point.VertexPoint.TextcoordVector, 1);
            //return point.FlatNormal;
            //return point.ShadedNormal;
            //return point.Tangente;
            //return -Vector3D.Normalize(Vector3D.Cross(point.Tangente, point.OrientedFlatNormal));
            //return point.BumpmapColor;
            //return new Vector3D(1, 1, 1) * (point.ShadedNormal * point.OrientedFlatNormal);
            //return Math.Max(0, Vector3D.Normalize(this.camera.Position - point.Position) * point.ShadedNormal) * new Vector3D(1, 1, 1); //Wenn hier schwarze Stellen zu sehen sind, stimmt was mit den Normalenrichtung nicht
            //return Math.Abs(Vector3D.Normalize(this.camera.Position - point.Position) * point.ShadedNormal) * new Vector3D(1, 1, 1);


            return GetColorWitherRasterizerFormel(new BrdfPoint(point, Vector3D.Normalize(point.Position - this.pixelRadianceCalculator.GlobalProps.Camera.Position), float.NaN, float.NaN), x, y, rand);
        }

        private Vector3D GetColorWitherRasterizerFormel(BrdfPoint point, int x, int y, IRandom rand)
        {
            if ((point.SurfacePoint.Propertys as ObjectPropertys).HasSilhouette && this.pixelRadianceCalculator.IsEdgePixel(x, y)) return new Vector3D(1, 0, 0);
            if ((point.SurfacePoint.Propertys as ObjectPropertys).CanReceiveLight == false) return point.SurfacePoint.Color;

            float diffusePortion = point.DiffusePortion;
            if (point.SurfacePoint.BrdfModel == BrdfModel.PlasticDiffuse) diffusePortion = 1;

            Vector3D diffuseColor = GetColorFromPixelShader(point.SurfacePoint);
            Vector3D reflectColor = GetReflectionColor(point, rand);
            return diffusePortion * diffuseColor + (1 - diffusePortion) * reflectColor;
        }

        private Vector3D GetColorFromPixelShader(IntersectionPoint point)
        {
            if ((point.Propertys as ObjectPropertys).CanReceiveLight == false) return point.Color;

            Vector3D sumColor = new Vector3D(0, 0, 0);
            Vector3D normalVector = point.ShadedNormal;
            float shadowFactor = 1;
            foreach (var L in this.lightSources)
            {
                if (L.CreateShadows && IsInShadow(point, L)) shadowFactor = 0.5f; // continue;

                Vector3D toLight = L.Position - point.Position;
                float dist = toLight.Length();
                float distanceFactor = 1 / (L.ConstantAttenuation +
                                           L.LinearAttenuation * dist +
                                           L.QuadraticAttenuation * dist * dist);

                toLight = Vector3D.Normalize(toLight);

                //Berechne Diffuse Farbe
                //Vector3D reflektetLight = Vector3D.Normalize(this.camera.Position - point.Position); //Vector3D.Normalize(LichtVektor - Vektor.Normiere(this.camera.Position - point.Position));
                float NdotL = Math.Max(normalVector * toLight, 0.0f);           // Diffuse Faktor
                float NdotS = Math.Max(normalVector * this.specularReflectDir, 0.0f);        // Glanzpunkt Faktor
                if ((point.Propertys as ObjectPropertys).ShowFromTwoSides && NdotL == 0)                       // Rückseite ist sichtbar?
                {
                    NdotL = Math.Max(-normalVector * toLight, 0.0f);            // Diffuse Faktor
                    NdotS = Math.Max(-normalVector * this.specularReflectDir, 0.0f);         // Glanzpunkt Faktor
                }

                float spot = 1;
                if (L.SpotCutoff != -1) //Punkt-Richtungslicht
                {
                    spot = Math.Max(L.SpotDirection * (-toLight), 0.0f);
                    if (spot < L.SpotCutoff) spot = 0;
                    //if (spot < (1.0f - L.SpotCutoff / 180)) spot = 0;
                    spot = (float)Math.Pow(spot, L.SpotExponent);
                }

                //Berechne größe des Glanzpunktes
                if (NdotS > 1.0f) NdotS = 1.0f;
                float specuFarbe = (float)Math.Pow(NdotS, point.SpecularHighlightPowExponent);
                if (point.SpecularHighlightPowExponent == 0) specuFarbe = 0;

                //sumColor += distanceFactor * (noLightColor * NdotL + new Vector3D(colorMaterialSpecular[0], colorMaterialSpecular[1], colorMaterialSpecular[2]) * specuFarbe);
                Vector3D ambientTerm = new Vector3D(0.1f, 0.1f, 0.1f);
                Vector3D diffuseTerm = point.Color * NdotL; 
                Vector3D specularTerm = new Vector3D(1,1,1) * specuFarbe;

                Vector3D contribution = Clamp(point.Color * 0.05f + distanceFactor * spot * (point.Color * 0.05f + diffuseTerm + specularTerm), 0, 1);

                //return PixelHelper.VectorToColor(noLightColor);
                //return PixelHelper.VectorToColor(new Vector3D(1, 1, 1) * NdotL);
                //return point.Tangente;
                //return normalVector;

                sumColor += contribution;
            }

            return sumColor * shadowFactor;
        }
        private static Vector3D Clamp(Vector3D f, float min, float max)
        {
            return new Vector3D(Clamp(f.X, min, max), Clamp(f.Y, min, max), Clamp(f.Z, min, max));
        }

        private static float Clamp(float f, float min, float max)
        {
            if (f < min) f = min;
            if (f > max) f = max;
            return f;
        }

        private bool IsInShadow(IntersectionPoint point, RasterLightSource light)
        {
            if (this.pixelRadianceCalculator.GlobalProps.ShadowsForRasterizer == RasterizerShadowMode.Shadowmap)
            {
                //Prüfe, dass p die Lichtquelle trifft
                var p = this.visibleTester.GetPointOnIntersectableLight(point, 0, light.Position, light.RayHeigh);
                return p == null;
            }else
            {
                //Prüfe, dass p ein Objekt trifft, wo HasStencilShadow==true ist 
                var p = this.intersectionFinderForStencilShadows.GetIntersectionPoint(new Ray(point.Position, Vector3D.Normalize(light.Position - point.Position)), 0, point.IntersectedObject);
                return p != null;
            }
        }

        private Vector3D GetReflectionColor(BrdfPoint point, IRandom rand)
        {
            if (point.DiffusePortion == 1 ||point.SurfacePoint.BrdfModel == BrdfModel.PlasticDiffuse) return new Vector3D(0, 0, 0);

            float refractionIndexCurrentMedium = 1;
            float refractionIndexNextMedium = point.SurfacePoint.RefractionIndex;

            BrdfPoint runningPoint = new BrdfPoint(point.SurfacePoint, point.DirectionToThisPoint, refractionIndexCurrentMedium, refractionIndexNextMedium);
            Vector3D pathWeight = new Vector3D(1, 1, 1);
            
            bool isOutside = true;
            BrdfSampleEvent newDirection;
            for (int i=0;i<this.pixelRadianceCalculator.GlobalProps.RecursionDepth;i++)
            {
                if (runningPoint.DiffusePortion == 1) break;

                //Sample Richtung ohne Absorbation
                var r = runningPoint.Brdf.SampleDirection(runningPoint.DirectionToThisPoint, rand.NextDouble(), rand.NextDouble(), 1); //u3 = Materialauswahl; Nimm immer den Refracted-Ray
                if (r == null) break;
                newDirection = new BrdfSampleEvent() { Brdf = r.BrdfWeightAfterSampling, ExcludedObject = runningPoint.SurfacePoint.IntersectedObject, RayWasRefracted = r.RayWasRefracted, Ray = new Ray(runningPoint.SurfacePoint.Position, r.SampledDirection) };

                if (runningPoint.SurfacePoint.BrdfModel == BrdfModel.TextureGlass)
                {
                    newDirection.Brdf = 0.3f * runningPoint.SurfacePoint.Color + 0.7f * runningPoint.SurfacePoint.Propertys.MirrorColor;
                }

                if (runningPoint.SurfacePoint.BrdfModel == BrdfModel.Mirror)
                {
                    //Benutzte die selbe Formel wie der Rasterizer beim Blending
                    float mirrorBlendfactor = 0.2f; //Wird beim Rasterizer im SpiegelViereck verwendet
                    newDirection.Brdf = (new Vector3D(1, 1, 1) - runningPoint.SurfacePoint.Color) * (1 - mirrorBlendfactor) + runningPoint.SurfacePoint.Color;
                }

                if (newDirection.RayWasRefracted) isOutside = !isOutside;                    
                
                
                if (isOutside)
                {
                    refractionIndexCurrentMedium = 1;
                    refractionIndexNextMedium = runningPoint.SurfacePoint.RefractionIndex;
                }
                else
                {
                    refractionIndexCurrentMedium = runningPoint.SurfacePoint.RefractionIndex;
                    refractionIndexNextMedium = 1;
                }
                if (newDirection.RayWasRefracted)
                {
                    //Die Fresnel-Gesetze sind nicht symmetrisch. Wenn z.B. 80% gebrochen werden und 20% reflektiert, dann hängt das vom Licht-Einstrahlwinkel ab.
                    //Wenn ich nun ein Pfad mit LightTracing erzeugt habe und ich will, dass die Fresnel-Brdf beim PathTracing für die gleichen Input-Output-Richtungen den gleichen
                    //Wert liefert, dann muss ich hiermit dafür sorgen, dass das Pathtracing so wie Lighttracing aussieht.
                    float relativeIOR = (refractionIndexCurrentMedium / refractionIndexNextMedium);
                    newDirection.Brdf *= (relativeIOR * relativeIOR);
                }

                pathWeight = Vector3D.Mult(pathWeight, newDirection.Brdf);

                var p = this.pixelRadianceCalculator.IntersectionFinder.GetIntersectionPoint(newDirection.Ray, 0, newDirection.ExcludedObject, float.MaxValue);
                if (p == null) break;

                if (isOutside)
                {
                    refractionIndexCurrentMedium = 1;
                    refractionIndexNextMedium = p.RefractionIndex;
                }
                else
                {
                    refractionIndexCurrentMedium = p.RefractionIndex;
                    refractionIndexNextMedium = 1;
                }

                runningPoint = new BrdfPoint(p, newDirection.Ray.Direction, refractionIndexCurrentMedium, refractionIndexNextMedium);

                
            }

            return Vector3D.Mult(pathWeight, runningPoint.DiffusePortion * GetColorFromPixelShader(runningPoint.SurfacePoint));
        }
    }

    class RasterLightSource : RasterizerLightSourceDescription
    {
        public Vector3D Position { get; private set; }
        public IIntersectableRayDrawingObject RayHeigh { get; private set; }

        public RasterLightSource(IIntersectableRayDrawingObject rayHeigh, Vector3D position)
            :base((rayHeigh.Propertys as ObjectPropertys).RasterizerLightSource)
        {
            this.Position = position;
            this.RayHeigh = rayHeigh;
            this.SpotCutoff = (float)Math.Cos(this.SpotCutoff * Math.PI / 180);
        }
    }
}

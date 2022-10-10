using GraphicMinimal;
using IntersectionTests;
using RaytracingBrdf.BrdfFunctions;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz;
using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Walter;
using System;

namespace RaytracingBrdf
{
    public class BrdfFactory
    {
        public static IBrdf CreateBrdf(IntersectionPoint point, Vector3D directionToPoint, float refractionIndexCurrentMedium, float refractionIndexNextMedium)
        {
            switch (point.BrdfModel)
            {
                case BrdfModel.Diffus:
                    //return new BrdfDiffuseUniformWeighted(point);
                    return new BrdfDiffuseCosinusWeighted(point);

                case BrdfModel.Mirror:
                    return new BrdfMirror(point, true);

                case BrdfModel.MirrorWithRust:
                    return new MirrorWithRustBrdf(point);

                case BrdfModel.TextureGlass:
                    return new BrdfGlas(point, directionToPoint, refractionIndexCurrentMedium, refractionIndexNextMedium, true);

                case BrdfModel.MirrorGlass:
                    return new BrdfGlas(point, directionToPoint, refractionIndexCurrentMedium, refractionIndexNextMedium, false);

                case BrdfModel.Phong:
                    return new DiffuseAndOtherBrdf(point, new BrdfGlossy(point), 0.1f);

                case BrdfModel.Tile:
                    return new DiffuseAndOtherBrdf(point, new BrdfMirror(point, false), point.Propertys.TileDiffuseFactor);

                case BrdfModel.FresnelTile:
                    return new MirrorAndOther(point, directionToPoint, new BrdfDiffuseCosinusWeighted(point));

                case BrdfModel.DiffuseAndMirror:
                    return new DiffuseAndMirrorWithSumBrdf(point);

                case BrdfModel.MicrofacetTile:
                    return new DiffuseAndOtherBrdf(point, new HeizMirror(point, directionToPoint), point.Propertys.TileDiffuseFactor);

                case BrdfModel.PlasticDiffuse:
                    return new DiffuseAndOtherBrdf(point, new BrdfSpecularHighlight(point, false), 0.6f);

                case BrdfModel.PlasticMirror:
                    //return new MirrorAndOther(point, directionToPoint, new DiffuseAndOtherBrdf(point, new BrdfGlanzpunkt(point, false), 0.6f));
                    return new DiffuseAndOtherBrdf(point, new TwoBrdfs(new BrdfSpecularHighlight(point, false), new BrdfMirror(point, false), 0.7f), 0.6f); //60% Diffuse; 28 % Glanzpunkt; 12% Mirror

                case BrdfModel.PlasticMetal:
                    //return new BrdfSpecularHighlight(point, true);
                    return new DiffuseAndOtherBrdf(point, new BrdfSpecularHighlight(point, true), 0.6f);

                case BrdfModel.WalterGlass:
                    return new WalterGlas(point, directionToPoint, refractionIndexCurrentMedium, refractionIndexNextMedium);

                case BrdfModel.WalterMetal:
                    return new WalterMirror(point, directionToPoint);

                case BrdfModel.HeizGlass:
                    return new HeizGlas(point, directionToPoint, refractionIndexCurrentMedium, refractionIndexNextMedium);

                case BrdfModel.HeizMetal:
                    return new HeizMirror(point, directionToPoint);

                case BrdfModel.DiffusePhongGlassOrMirrorSum:
                    return new DiffusePhongGlasOrMirrorSum(point, directionToPoint, refractionIndexCurrentMedium, refractionIndexNextMedium);
            }

            throw new Exception("Unbekanntes Material: " + point.BrdfModel);
        }
    }
}

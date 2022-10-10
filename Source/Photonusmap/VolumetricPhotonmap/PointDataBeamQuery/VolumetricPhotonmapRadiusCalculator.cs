using System.Collections.Generic;
using System.Linq;
using GraphicMinimal;
using ParticipatingMedia;
using RayCameraNamespace;
using SubpathGenerator;

namespace Photonusmap
{
    public static class VolumetricPhotonmapRadiusCalculator
    {
        public static float GetSearchRadiusFromLighPathList(List<SubPath> lightPaths, IRayCamera rayCamera)
        {            
            var particle = lightPaths.SelectMany(x => x.Points).Where(x => x.LocationType == MediaPointLocationType.MediaParticle).Select(x => x.Position).ToList();

            if (particle.Any() == false) return 1;

            Vector3D min = new Vector3D(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3D max = new Vector3D(float.MinValue, float.MinValue, float.MinValue);
            foreach (var P in particle)
            {
                if (P.X < min.X) min.X = P.X;
                if (P.Y < min.Y) min.Y = P.Y;
                if (P.Z < min.Z) min.Z = P.Z;
                if (P.X > max.X) max.X = P.X;
                if (P.Y > max.Y) max.Y = P.Y;
                if (P.Z > max.Z) max.Z = P.Z;
            }
            Vector3D center = min + (max - min) / 2;
            float radius = rayCamera.GetPixelFootprintSize(center).X;

            return radius;
        }
    }
}

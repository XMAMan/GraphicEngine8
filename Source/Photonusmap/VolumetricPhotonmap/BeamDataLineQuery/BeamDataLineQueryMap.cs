using GraphicGlobal;
using IntersectionTests;
using IntersectionTests.BeamLine;
using SubpathGenerator;
using System.Collections.Generic;
using System.Linq;

namespace Photonusmap
{
    public class BeamDataLineQueryMap
    {
        public float LightPathCount { get; private set; } //Durch den Reducion-Faktor kann es auch passieren, dass diese Zahl kleiner 1 ist und aufgrund von Zufall trotzdem ein paar Zylinder in der Map sind
        public float SearchRadius { get; private set; } //Allen Strahlen(Cylindern) wird einmal ein Radius zugewiesen. Den Radius im Nachhinein zu ändern ist dann nicht mehr möglich

        private readonly IBeamLineIntersector intersector;

        public BeamDataLineQueryMap(List<SubPath> lightPaths, int sendedLightPathCount, float searchRadius, float photonCountReductionFactor, IRandom rand)
        {
            this.LightPathCount = (sendedLightPathCount * photonCountReductionFactor);
            this.SearchRadius = searchRadius;

            var beams = ReduceLightPathList(lightPaths, sendedLightPathCount, photonCountReductionFactor, rand).SelectMany(x => x.Points).Where(x => x.LineToNextPoint != null && x.LineToNextPoint.HasScattering()).Select(x => new BeamRay(x, x.LineToNextPoint, searchRadius)).Cast<IIntersectableCylinder>().ToList();

            if (beams.Count < 100)
            {
                this.intersector = new EasyBeamLineIntersector(beams);
            }else
            {
                this.intersector = new CylinderGrid(beams, 64); //Das Grid rentiert sich erst ab einer bestimmten Anzahl. Davor ist lineare Suche schneller
            }
        }

        private List<SubPath> ReduceLightPathList(List<SubPath> lightPaths, int photonCount, float photonCountReductionFactor, IRandom rand)
        {
            float f = lightPaths.Count / (float)photonCount * photonCountReductionFactor;
            return lightPaths.Where(x => rand.NextDouble() < f).ToList();
        }

        public List<LineBeamIntersectionPoint> QuerryBeamRays(IQueryLine queryLine)
        {
            return this.intersector.GetAllIntersectionPoints(queryLine);
        }
    }
}

using System.Collections.Generic;
using GraphicMinimal;
using IntersectionTests;
using GraphicGlobal;
using RaytracingBrdf.SampleAndRequest;

namespace SubpathGenerator
{
    //Enthält alle Daten die beim Subpath-Erstellen benötigt werden
    class RayWalkData
    {
        //Daten für den ganzen Subpath
        public float PathCreationTime { get; private set; }
        public bool IsEyePath { get; private set; }
        public bool LightSourceIsInfinityAway { get; private set; } = false; //Nur bei ein LightSubpath, der per Richtungs- oder Umgebungslicht gesampelt wurde ist das true
        public List<PathPoint> Points { get; private set; } = new List<PathPoint>();


        //Daten für ein einzelnen Subpath-Punkt (Hier wird nur schreibend vom StandardSubPathPointsSampler oder MediaSubPathPointsSampler zugegriffen)
        public BrdfSampleEvent SampleEvent;
        public Vector3D PathWeight;            
        public float RefractionIndexCurrentMedium = 1;
        public float RefractionIndexNextMedium = float.NaN;

        //Getter
        public Ray Ray { get { return this.SampleEvent.Ray; } }
        public Vector3D RayDirection { get { return this.SampleEvent.Ray.Direction; } }
        public IIntersecableObject ExcludedObject { get { return this.SampleEvent.ExcludedObject; } }

        private RayWalkData(BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, bool isEyePath, bool lightSourceIsInfinityAway)
        {
            this.SampleEvent = sampleEvent;
            this.PathWeight = pathWeight;
            this.PathCreationTime = pathCreationTime;
            this.IsEyePath = isEyePath;
            this.LightSourceIsInfinityAway = lightSourceIsInfinityAway;
        }

        public static RayWalkData CreateEyePathData(BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, PathPoint cameraPoint)
        {
            var rayWalkData = new RayWalkData(sampleEvent, pathWeight, pathCreationTime, true, false);
            cameraPoint.PdfA = 1;
            rayWalkData.Points.Add(cameraPoint);
            return rayWalkData;
        }

        public static RayWalkData CreateLightPathData(BrdfSampleEvent sampleEvent, Vector3D pathWeight, float pathCreationTime, bool lightSourceIsInfinityAway, PathPoint lightPoint, float positionPdfA)
        {
            var rayWalkData = new RayWalkData(sampleEvent, pathWeight, pathCreationTime, false, lightSourceIsInfinityAway);
            lightPoint.PdfA = positionPdfA;
            rayWalkData.Points.Add(lightPoint);
            return rayWalkData;
        }

        public RayWalkData(RayWalkData copy)
        {
            this.PathCreationTime = copy.PathCreationTime;
            this.IsEyePath = copy.IsEyePath;
            this.LightSourceIsInfinityAway = copy.LightSourceIsInfinityAway;
            this.Points = copy.Points;            

            this.SampleEvent = copy.SampleEvent;
            this.PathWeight = copy.PathWeight;           
            this.RefractionIndexCurrentMedium = copy.RefractionIndexCurrentMedium;
            this.RefractionIndexNextMedium = copy.RefractionIndexNextMedium;
        }
    }
}

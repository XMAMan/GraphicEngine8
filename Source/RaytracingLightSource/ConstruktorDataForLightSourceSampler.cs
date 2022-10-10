using System;
using System.Collections.Generic;
using IntersectionTests;
using System.Threading;
using RayCameraNamespace;
using RayObjects.RayObjects;

namespace RaytracingLightSource
{
    public class ConstruktorDataForLightSourceSampler
    {
        public List<IRayObject> LightDrawingObjects;
        public IntersectionFinder IntersectionFinder; //Die IntersectionFinder werden für das Umgebungslicht benötigt um den Scenenradius zu
        public MediaIntersectionFinder MediaIntersectionFinder;//bestimmen und um ImportancePhotonen auszusenden
        public IRayCamera RayCamera; //Wird beim ImportancePhotonenSampler benötigt
        public int LightPickStepSize = 0; //0 = Lege LightPickProb nach Emission fest
        public Action<string, float> ProgressChangedHandler;
        public CancellationTokenSource StopTriggerForColorEstimatorCreation;        
    }
}

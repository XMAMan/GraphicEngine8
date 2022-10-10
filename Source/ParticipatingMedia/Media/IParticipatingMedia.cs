using GraphicGlobal;
using GraphicMinimal;
using ParticipatingMedia.DistanceSampling;
using ParticipatingMedia.Media.DensityField;
using ParticipatingMedia.PhaseFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticipatingMedia.Media
{
    //ExtinctionCoeffizient = Scattering-Coeffizient + Absorbatin-Coeffizient Sigma_t = Sigma_s + Sigma_a
    //Alle Angaben hier werden im Ray-Space beschrieben. D.h. ich gebe eine Menge von 3D-Punkten, welche alle auf einer Linie liegen dadurch an,
    //indem ich sie über (Ray ray, float rayMin, float rayMax) beschreibe. Die Punkte liegen dann auf ray.Start+ray.Direction*rayMin bis ray.Start+ray.Direction*rayMax
    //Die Absorbation- und Scattering-Koeffizienten sind Unnormierte Dichtefunktionen im Bezug auf das Volumenmaß. Einheit [m^-3]. Das Integral
    //über das gesamte Medium gibt an, wie viele Scatterteilchen/Absorbation-Teilchen das Medium enthält.
    //Ein IParticipatingMedia ist eine 3D-Punktwolke, welche aus Scattering- und Absorbation-Teilchen besteht.
    public interface IParticipatingMedia //Dieses Interface benutzt das VolumenSegment und die PhaseFunction (Samplen/Abfragen)
    {
        //Pfaddurchsatz auf gerader Strecke (Muss vermutlich in ein OpticalDeepth-Numeric-Integrator ausgelagert werden, welcher den Scatter+Absorbation-Coeffizient benutzt)
        Vector3D EvaluateAttenuation(Ray ray, float rayMin, float rayMax); //Gibt an, wie viel Prozent der Radiance beim Durchlaufen durchs Medium durchgelassen wird. Die 3D-Linie wird durch ray.Start+ray.Direction*rayMin bis ray.Start+ray.Direction*rayMax beschrieben
        Vector3D EvaluateEmission(Ray ray, float rayMin, float rayMax); //Gibt an, wie viel Radiance das Medium erzeugt (Wenn Media ein Feuer/Lichtquelle ist)

        Vector3D GetScatteringCoeffizient(Vector3D position);
        Vector3D GetAbsorbationCoeffizient(Vector3D position);

        IPhaseFunction PhaseFunction { get; }
        IDistanceSampler DistanceSampler { get; }

        bool HasScatteringSomeWhereInMedium(); //Hat dieses Medium prinzipiell irgendwo in der Medium-Wolke Scatterteilchen?
        bool HasScatteringOnPoint(Vector3D point);        

        int Priority { get; }
        float RefractionIndex { get; }
    }

    public interface IInhomogenMedia : IParticipatingMedia, IMediaOnWaveLength //Wid vom CounpoundPariticpatingMedia benutzt 
    { }

    public class ParticipatingMediaBuilder
    {
        public IParticipatingMediaFactory mediaFactory = null;

        public ParticipatingMediaBuilder()
        {
            this.mediaFactory = new StandardParticipatingMediaFactory();
        }

        public ParticipatingMediaBuilder(IParticipatingMediaFactory mediaFactory)
        {
            this.mediaFactory = mediaFactory;
        }


        public IParticipatingMedia CreateGlobalMedia(IParticipatingMediaDescription mediaDescription)
        {
            int priority = 0;//0 = Priorität von der Gesamtszene (Luft)
            return mediaFactory.CreateFromDescription(null, mediaDescription, priority, 1);
        }

        //Erzeugt für den Innenraum von ein DrawingObject das Media
        public IParticipatingMedia CreateMediaForDrawingObject(DrawingObject drawingObject, int priority)
        {
            BoundingBox box = drawingObject.DrawingProps.MediaDescription != null ? drawingObject.GetBoundingBoxFromObject() : null;
            return mediaFactory.CreateFromDescription(box, drawingObject.DrawingProps.MediaDescription, priority, drawingObject.DrawingProps.RefractionIndex);
        }

        //Media-Prioität
        //0 ..  Die Luft von der Szene
        //1 ..  Alle NoMedia-Objekte
        //2 ..* All Media-Objekte 
        public static int GetMediaPriorityFromDrawingObject(DrawingObject drawingObject, List<DrawingObject> drawingObjects)
        {
            //if (drawingObject.DrawingProps.MediaDescription == null && float.IsNaN(drawingObject.DrawingProps.RefractionIndex) == false && drawingObject.DrawingProps.RefractionIndex != 1)
            //{
                //Glaswürfel ohne Media braucht Vacuum-Media mit Prirität > 2, wenn noch ein umschließender Luftwürfel vorhanden ist. Das bekommt er nur, wenn ich hier die MediaDescription befülle
                //drawingObject.DrawingProps.MediaDescription = new DescriptionForVacuumMedia();
            //}

            if (drawingObject.DrawingProps.MediaDescription == null) return 1;
            int priority = drawingObject.DrawingProps.MediaDescription != null ? drawingObject.DrawingProps.MediaDescription.Priority : 0;
            if (priority == 0) //Vergebe automatisch Aufsteigend nach Reihenfolge (Darf nur gemmacht werden, wenn MediaObjekte sich nicht überlappen/verschachtelt sind)
            {
                var mediaObjects = drawingObjects.Where(x => x.DrawingProps.MediaDescription != null).ToList();
                return mediaObjects.IndexOf(drawingObject) + 2;
            }else
            {
                if (priority < 2) throw new Exception("Die Priorität muss größer gleich 2 sein");
                var groups = drawingObjects.Where(x => x.DrawingProps.MediaDescription != null).GroupBy(x => x.DrawingProps.MediaDescription.Priority);
                //if (groups.Any(x => x.Count() != 1)) throw new Exception("Jede Priorität darf nur einmal vergeben weren");
                return priority;
            }
            
        }
    }

    public interface IParticipatingMediaFactory
    {
        //box = BoundingBox in Worldspace von dem Drawing-Objekt, was das Medium enthält
        IParticipatingMedia CreateFromDescription(BoundingBox box, IParticipatingMediaDescription mediaDescription, int priority, float refractionIndex);
    }

    public class StandardParticipatingMediaFactory : IParticipatingMediaFactory
    {
        public IParticipatingMedia CreateFromDescription(BoundingBox box, IParticipatingMediaDescription mediaDescription, int priority, float refractionIndex)
        {
            if (mediaDescription != null)
            {
                if (mediaDescription is DescriptionForVacuumMedia) return new ParticipatingMediaVacuum(priority, refractionIndex);
                if (mediaDescription is DescriptionForHomogeneousMedia) return new ParticipatingMediaHomogen(priority, refractionIndex, mediaDescription as DescriptionForHomogeneousMedia);
                if (mediaDescription is DescriptionForSkyMedia) return ParticipatingMediaSky.CreateInstance(priority, refractionIndex, mediaDescription as DescriptionForSkyMedia);
                //if (mediaDescription is DescriptionForSkyMedia) return new ParticipatingMediaRayleighSky(priority, refractionIndex, mediaDescription as DescriptionForSkyMedia); //Wenn ich nur die Rayleigh-Partikel vom Himmel sehen will
                //if (mediaDescription is DescriptionForSkyMedia) return new ParticipatingMediaMieSky(priority, refractionIndex, mediaDescription as DescriptionForSkyMedia); //Wenn ich nur die Mie-Partikel vom Himmel sehen will

                //if (mediaDescription is DescriptionForCloudMedia) return new ParticipatingMediaDensityField(new CloudDensityField(box, mediaDescription as DescriptionForCloudMedia), priority, refractionIndex, mediaDescription as DescriptionForDensityFieldMedia); //So erscheint der gesamte Wolkenbereich, wo die Density 0 ist, heller (falsch)
                if (mediaDescription is DescriptionForCloudMedia) return new CompoundParticipatingMedia(new ParticipatingMediaDensityField(new CloudDensityField(box, mediaDescription as DescriptionForCloudMedia), priority, refractionIndex, mediaDescription as DescriptionForDensityFieldMedia), ParticipatingMediaSky.CreateInstance(priority, refractionIndex, new DescriptionForSkyMedia()));
                if (mediaDescription is DescriptionForRisingSmokeMedia) return new ParticipatingMediaDensityField(new RisingSmokeDensityField(box, mediaDescription as DescriptionForRisingSmokeMedia), priority, refractionIndex, mediaDescription as DescriptionForDensityFieldMedia);
                if (mediaDescription is DescriptionForRisingSmokeMedia1) return new ParticipatingMediaDensityField(new RisingSmokeDensityField1(box, mediaDescription as DescriptionForRisingSmokeMedia1), priority, refractionIndex, mediaDescription as DescriptionForDensityFieldMedia);
                if (mediaDescription is DescriptionForGridCloudMedia) return new ParticipatingMediaDensityField(new GridCloudDensityField(box, mediaDescription as DescriptionForGridCloudMedia), priority, refractionIndex, mediaDescription as DescriptionForDensityFieldMedia);

                throw new Exception("Unknown media " + mediaDescription.GetType());
            }
            else
            {
                return new ParticipatingMediaVacuum(priority, refractionIndex);
            }
        }
    }
}

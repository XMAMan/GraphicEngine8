using System;
using GraphicGlobal;
using GraphicMinimal;

namespace ParticipatingMedia.DistanceSampling
{
    //Es gibt zwei Arten von Distanzsampling: 
    //SampleRayPositionWithPdfFromRayMinToInfinity = Wird beim Subpath-Erzeugen benutzt. Der Definitionsbereich der Pdf geht von 0 bis Unendlich
    //SampleRayPositionWithPdfFromRayMinToRayMax = Wird beim Fullpath-DirectLightingOnEdge benutzt (Wenn ich auf ein gegebenen Segment ein Zufallspunkt bestimmen will). Der Definitionsbereich der Pdf geht von 0 bis (RayMax - RayMin)
    public interface IDistanceSampler
    {
        //Wenn startpointIsInMedium == false ist, dann heißt das, der Strahl dringt vom Media-Border-Punkt gerade in das Medium ein
        //Wenn startpointIsInMedium == true, dann startet der Strahl von ein MediaParticel von den Medium, dem der DistanceSampler zugeordnet ist
        RaySampleResult SampleRayPositionWithPdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, IRandom rand, bool startPointIsOnParticleInMedia); //Sampelt eine Distanz, welchen zwischen rayMin und rayMax liegt
        DistancePdf GetSamplePdfFromRayMinToInfinity(Ray ray, float rayMin, float rayMax, float sampledRayPosition, bool startPointIsOnParticleInMedium, bool endPointIsOnParticleInMedium); //sampledRayPosition liegt zwischen rayMin und rayMax

        RaySampleResult SampleRayPositionWithPdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, IRandom rand);
        DistancePdf GetSamplePdfFromRayMinToRayMax(Ray ray, float rayMin, float rayMax, float sampledRayPosition);
    }

    //Der Grund warum ich hier keine Attenutation durch PdfL zurück gebe? Weil es ja 3 unterschiedliche Attenuations gibt und ich aber nur nach einer von den
    //3 Wellenlänge importancsample. Somit kürzt sich lediglich einer von den 3 Faktoren weg und es lohnt sich somit nicht diese Division vorzuziehen.
    public class RaySampleResult
    {
        public float RayPosition = float.NaN; //Zufallszahl zwischen RayMin und RayMax
        public float PdfL = float.NaN; //Wenn RayPosition < RayMax, dann ist das hier eine Wahrscheinlichkeitsdichte im Bezug auf das Längenmaß. 
                           //Ansonsten die Wahrscheinlichkeit ohne Scattering oder Absorbation durchs Medium zu gelangen.
        public float ReversePdfL = float.NaN;//Distance-Samplingwahrscheinlichkeit in die Gegenrichtung. Sie ist nur dann das gleiche wie die PdfL,
                                             //wenn start- und end-Punkt im Medium liegt und keine Mediumgrenze durchschritten wurde. Wird eine Mediagrenze
                                             //durchschritten, so ist eine PdfL der Ends-On-Media-Particel und die andere der Go-Through-Media-Border-Fall.
    }
    
    public class DistancePdf
    {
        public float PdfL = float.NaN;
        public float ReversePdfL = float.NaN;
    }
}

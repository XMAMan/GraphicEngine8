using SubpathGenerator;
using GraphicMinimal;
using IntersectionTests;
using ParticipatingMedia;
using System.Text;
using System;
using FullPathGenerator.AnalyseHelper;
using RayTracerGlobal;

namespace FullPathGenerator
{
    //Eine Kette von Punkten, dessen erster Punkt auf der Kamera und dessen letzter Punkt auf der Lichtquelle liegt
    //Die Richtung von Kamera zu Lichtquelle ist die Pathtracing-Richtung. Die entgegengesetzte Richtung ist die Lighttracing-Richtung
    public class FullPath
    {
        public Vector3D PathContribution { get; private set; } //Radiance ohne Mis-Gewicht
        public double PathPdfA { get; private set; } //Unter dieser PdfA wurde der Path gesampelt
        public FullPathPoint[] Points { get; private set; } //Der 1. Punkt liegt auf der Kamera und der letzte auf einer Lichtquelle
        public Vector2D PixelPosition = null;
        public float MisWeight = float.NaN;
        public Vector3D Radiance { get; set; } //Mis-Gewichtete PathContribution
        public IFullPathSamplingMethod Sampler { get; set; } = null; //Von diesen Sampler wurde der Pfad erzeugt
        public SamplingMethod SamplingMethod => this.Sampler.Name;  //Wird für den ImageFullPathAnalyser benötigt

        public FullPath(Vector3D pathContribution, double pathPdfA, FullPathPoint[] points, IFullPathSamplingMethod sampler)
        {
            this.PathContribution = pathContribution;
            this.PathPdfA = pathPdfA;
            this.Points = points;
            this.Sampler = sampler;
        }

        public FullPath(FullPath copy)
        {
            this.PathContribution = copy.PathContribution;
            this.PathPdfA = copy.PathPdfA;
            this.Points = copy.Points;
            this.PixelPosition = copy.PixelPosition;
            this.MisWeight = copy.MisWeight;
            this.Radiance = copy.Radiance;
            this.Sampler = copy.Sampler;
        }

        public bool IsSurfacePathOnly()
        {
            for (int i = 1; i < this.Points.Length; i++)
                if (this.Points[i].LocationType != MediaPointLocationType.Surface && this.Points[i].LocationType != MediaPointLocationType.MediaBorder) return false;
            return true;
        }

        public string GetPathSpaceString()
        {
            return FullPathToPathSpaceConverter.ConvertPathToPathSpaceString(this);
        }

        public string GetLocationAndPathWeightInformation()
        {
            StringBuilder str = new StringBuilder();
            for (int i=0;i<this.Points.Length;i++)
            {
                str.AppendLine("SubPathIndex=" + this.Points[i].Point.Index);
                str.AppendLine("LocationType=" + this.Points[i].LocationType);

                if (this.Points[i].Point.SurfacePoint != null && this.Points[i].Point.SurfacePoint.IntersectedObject != null)
                    str.AppendLine("LocationName=" + this.Points[i].Point.SurfacePoint.Propertys.Name);

                //PathWeight = Brdf / continuationPdf * Attenuation / LinePdf
                //Brdf = SimpleBrdf * Cos / PdfW * Albedo
                str.AppendLine("PathWeight=" + this.Points[i].Point.PathWeight);

                if (this.Points[i].Point.BrdfSampleEventOnThisPoint != null)
                {
                    str.AppendLine("Brdf=" + this.Points[i].Point.BrdfSampleEventOnThisPoint.Brdf);
                    str.AppendLine("PdfW=" + this.Points[i].Point.BrdfSampleEventOnThisPoint.PdfW);
                    if (this.Points[i].Point.BrdfSampleEventOnThisPoint.RayWasRefracted)
                        str.AppendLine("RayWasRefracted=" + this.Points[i].Point.BrdfSampleEventOnThisPoint.RayWasRefracted);
                }                
                if (this.Points[i].Point.BrdfPoint != null)
                {
                    float continuationPdf = Math.Min(1, Math.Max(MagicNumbers.MinSurfaceContinuationPdf, this.Points[i].Point.BrdfPoint.ContinuationPdf));
                    str.AppendLine("continuationPdf=" + continuationPdf);
                }

                if (i < this.Points.Length -1)
                {
                    Vector3D p1toP2 = this.Points[i + 1].Position - this.Points[i].Position;
                    Vector3D p1toP2Dir = Vector3D.Normalize(p1toP2);
                    float cos1 = 1, cos2 = 1;
                    if (this.Points[i].LocationType == MediaPointLocationType.MediaBorder ||
                        this.Points[i].LocationType == MediaPointLocationType.Surface)
                    {
                        float swap = 1;
                        if (this.Points[i].Point.BrdfSampleEventOnThisPoint != null && this.Points[i].Point.BrdfSampleEventOnThisPoint.RayWasRefracted) swap = -1;
                        cos1 = Math.Max(0, (this.Points[i].Point.SurfacePoint.ShadedNormal * swap) * p1toP2Dir);
                    }

                    if (this.Points[i + 1].LocationType == MediaPointLocationType.MediaBorder ||
                        this.Points[i + 1].LocationType == MediaPointLocationType.Surface)
                    {
                        cos2 = Math.Max(0, this.Points[i + 1].Point.SurfacePoint.ShadedNormal * (-p1toP2Dir));
                    }

                    float geometryTerm = cos1 * cos2 / p1toP2.SquareLength();

                    str.AppendLine($"Geometry-Term_{i}_to_{i + 1} ({cos1} * {cos2} / {p1toP2.SquareLength()}) = {geometryTerm}");
                    if (this.Points[i].EyeLineToNext != null)
                    {
                        str.AppendLine($"Attenuation = { this.Points[i].EyeLineToNext.AttenuationWithoutPdf()}");
                        str.AppendLine($"LinePdf = { this.Points[i].EyeLineToNext.SampledPdfL().PdfL}");
                    }
                }
                

                str.AppendLine("..............");
            }
            return str.ToString();
        }

        public override string ToString()
        {
            return this.SamplingMethod + " " + (this.PixelPosition != null ? this.PixelPosition.ToString() : "null") + " " + this.Radiance;
        }

        public int PathLength { get { return this.Points.Length; } }
    }

    //Gibt an, auf welche Weise der Brdf-Wert von ein Fullpath-Point erzeugt wurde
    //Über diese Info kann ich ermitteln, ob ein Fullpath-Punkt, welcher ein Verbundmaterial (Diffuse+Spekular) ist, Diffuse oder Spekular ist
    public enum BrdfCreator
    {
        BrdfSampling,   //Über den Brdf-Sampler vom Subpath-Creator
        BrdfEvaluation, //Über den Fullpath-Sampler, welcher die Brdf-Abfrage nutzt (DirectLighting/VertexConnection)
        MergingPoint    //Über den Fullpath-Sampler beim Photonmapping(Surface) oder Point2Point-Merging (Media)
    }

    public class FullPathPoint
    {
        public double EyePdfA { get; set; }       // Pathtracing
        public double LightPdfA { get; set; }     // Lighttracing
        public MediaLine EyeLineToNext { get; set; }    // Zeigt von diesen Punkt in Pathtracing-Richtung zum nächsten Punkt
        public MediaLine LightLineToNext { get; set; }  // Zeigt von diesen Punkt in Lighttracing-Richtung zum vorherigen Punkt

        public float EyePdfWOnThisPoint { get; set; } = float.NaN;     //PdfW von diesen Punkt in Pathtracing-Richtung zum nächsten Punkt
        public float LightPdfWOnThisPoint { get; set; } = float.NaN;   //PdfW von diesen Punkt in Lighttracing-Richtung zum vorherigen Punkt

        public PathPoint Point { get; set; }
        public bool IsDiffusePoint { get; private set; } //Ist true, wenn das BrdfSample-Event diffuse gesampelt wurde oder kein BrdfSampling erfolgte (BrdfAbfrage am VertexConnection/VertexMerging-Punkt)

        //Für den FullPathTester
        public bool IsMergingPoint { get; set; } = false; //Wenn ein Fullpathsampler ein PathPoint erzeugt, dann kann er hiermit festlegen, ob die LineToNextPoint-Property von und zu diesen Punkt korrekt ist oder ein Bias enthält
        public bool PdfWContainsNumericErrors { get; set; } = false; //Wenn von diesen Punkt aus zu ein Partikel gezeigt wird, welches durch ein FullPathSampler erzeugt wurde, dann ist das hier true. Die PdfW und PdfWReverse enthält dann Fehler da sich die Richtung zum nächsten Punkt geändert hat


        public FullPathPoint(PathPoint point, MediaLine eyeLineToNext, MediaLine lightLineToNext, float eyePdfWOnThisPoint, float lightPdfWOnThisPoint, BrdfCreator brdfCreator)
        {
            this.Point = point;
            this.EyeLineToNext = eyeLineToNext;
            this.LightLineToNext = lightLineToNext;
            this.EyePdfWOnThisPoint = eyePdfWOnThisPoint;
            this.LightPdfWOnThisPoint = lightPdfWOnThisPoint;

            this.IsDiffusePoint = point.IsDiffusePoint; //Enthält das Material ein diffusen Anteil?

            //Der Fullpath-Sampler legt bei ein Verbundmaterial fest ob es diffuse oder spekular ist (Schau bei Brdf-Gesampelten Punkten auf das Sample-Event)
            if (brdfCreator == BrdfCreator.BrdfSampling && point.BrdfSampleEventOnThisPoint != null && point.BrdfSampleEventOnThisPoint.IsSpecualarReflected)
                this.IsDiffusePoint = false; //Wurde bei Verbundmaterial spekular gesampelt, dann wird IsDiffuse von true auf false geändert

            this.IsMergingPoint = brdfCreator == BrdfCreator.MergingPoint;
        }

        public Vector3D Position
        {
            get
            {
                return this.Point.Position;
            }
        }

        public bool IsSpecularSurfacePoint
        {
            get
            {
                return this.Point.IsSpecularSurfacePoint;
            }
        }

        public MediaPointLocationType LocationType
        {
            get
            {
                return this.Point.LocationType;
            }
        }

        public bool IsLocatedOnLightSource
        {
            get
            {
                return this.Point.IsLocatedOnLightSource;
            }
        }

        public bool IsLocatedOnLightSourceWhichCanBeHitViaPathtracing
        {
            get
            {
                return this.Point.IsLocatedOnLightSourceWhichCanBeHitViaPathtracing;
            }
        }

        public override string ToString()
        {
            if (this.Point == null) return base.ToString();
            if (this.Point.IsLocatedOnLightSource) return "LightSource";
            return this.Point.LocationType.ToString();
        }
    }
}

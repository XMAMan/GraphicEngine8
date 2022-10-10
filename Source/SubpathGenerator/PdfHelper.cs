using System;
using GraphicMinimal;
using ParticipatingMedia;
using RayTracerGlobal;

namespace SubpathGenerator
{
    //Hier sind all die übelst komplizierten Formeln fürs Multiple Importance Sampling / Rechnen mit Wahrscheinlichkeitsdichten im Bezug zu ein Flächen/Volumen-Maß
    //Wer diese Funktionen hier verstanden hat, der hat stochastisches Raytracing verstanden.
    //Wer hier Probleme hat, der sollte sich Eric-Veach Thesis, VCM (Vertex Connection & Merging) und UPBP (Unifying points, beams, and paths in volumetric light transport simulation) anschauen. 
    //Pdf = Wahrscheinlichkeitsdichte
    //W = Differentialer Flächeninhalt von ein Kugelsegment, was sich über W=dPhi * dTheta * Sin(theta) berechnet
    //A = Differentialer Flächeninhalt von einer 3D-Ebene (A=du*dv)
    //V = Differentiales Volumenelement (V=dx*dy*dz)
    //PdfW(Direction) = Wahrscheinlichkeitsdichte im Kugelsegmentmaß = Wahrscheinlichkeit eine Richtung zu erzeugen, welche in Richtung 'Direction' zeigt geteilt durch das differentiale Kugelsegment, durch das diese Richtung geht
    //PdfA(3D_SurfacePoint) = Wahrscheinlichkeitsdichte im 3D-Ebene-Flächenmaß =  Wahrscheinlichkeit ein Punkt auf einer 3D-Ebene zu erzeugen, welcher an der Stelle 3D_SurfacePoint liegt geteilt durch den differntialen Flächeninhalt, der um diesen Punkt herrum liegt
    //PdfV(3D_MediaPoint) = Wahrscheinlichkeitsdichte im Volumeninhaltsmaß = Wahrscheinlichkeit ein Punkt in einer Partikelwolke zu erzeugen, welcher an der Stelle 3D_MediaPoint liegt geteilt durch das differntiale Volumenelement, was diesen Punkt umschließt
    //PdfWtoA = Nimmt die PdfW; Multipliziert mit W und macht daraus eine Pdf; Dann dividiert es durch A (Wird bestimmt indem W auf die 3D-Ebene projetziert wird, welche in Richtung Direction liegt) und macht daraus eine PdfA
    public static class PdfHelper
    {
        //Lektion am 3.3.2016: Wenn man die PdfW nicht in eine PdfA umrechnet, sieht VCM schlecht aus (Bei Scene 0)
        //PdfW = Wahrscheinlichkeit, von Punkt x nach Punkt y eine Richtung zu generieren (Wird immer dann genommen, wenn ich die Brdf gesampelt habe und eine PdfW berechent habe)
        //brdfPdfW = Brdf-Pdf am Punkt x
        private static float PdfWtoA(float brdfPdfW, PathPoint brdfPosition, PathPoint lightPoint) //Umrechnung einer Pdf W.r.t Solid Angle in einer Pdf W.r.t Survace Area dP / dA
        {
            Vector3D lightToBrdf = brdfPosition.Position - lightPoint.Position;
            float distSqr = lightToBrdf.SquareLength(); //Das hier ist mein Notfallplan, falls die Normierung des lightToBrdf-Vektors eine Nulldivision verursacht
            float distLength = (float)Math.Sqrt(distSqr);
            if (distLength == 0)
            {
                return brdfPdfW / MagicNumbers.MinAllowedPathPointSqrDistance;
                //return Math.Max(1e-10f, brdfPdfW / MagicNumbers.MinAllowedPathPointSqrDistance);
            }
            //float distSqr = Math.Max(lightToBrdf.QuadratBetrag(), MagicNumbers.MinAllowedPathPointSqrDistance);

            return brdfPdfW * Math.Abs((lightToBrdf / distLength) * lightPoint.SurfacePoint.OrientedFlatNormal) / Math.Max(distSqr, MagicNumbers.MinAllowedPathPointSqrDistance);

            //return brdfPdfW * Math.Abs(Vector3D.Normalize(lightToBrdf) * lightPoint.Normal) / distSqr; //http://graphics.stanford.edu/papers/veach_thesis/thesis.pdf Seite 254
        }

        //Für Richtungs- und Umgebungslicht soll keine distSqr-Division erfolgen damit sie immer gleichhell leuchtet unabhängig vom Abstand
        private static float PdfWtoANoSqrtDivision(float brdfPdfW, PathPoint brdfPosition, PathPoint lightPoint) //Umrechnung einer Pdf W.r.t Solid Angle in einer Pdf W.r.t Survace Area dP / dA
        {
            Vector3D lightToBrdf = brdfPosition.Position - lightPoint.Position;
            float distSqr = lightToBrdf.SquareLength(); //Das hier ist mein Notfallplan, falls die Normierung des lightToBrdf-Vektors eine Nulldivision verursacht
            float distLength = (float)Math.Sqrt(distSqr);
            if (distLength == 0)
            {
                return brdfPdfW;
            }

            return brdfPdfW * Math.Abs((lightToBrdf / distLength) * lightPoint.SurfacePoint.OrientedFlatNormal);
        }


        //Vom brdfPosition aus wird mit der PdfW 'brdfPdfW' Richtung mediaPoint gesampelt. Dieser Funktion gibt die PdfV für den mediaPoint zurück
        public static float PdfWtoV(float brdfPdfW, Vector3D brdfPosition, Vector3D mediaPoint) //Umrechnung einer Pdf W.r.t Solid Angle in einer Pdf W.r.t Survace Area dP / dV
        {
            Vector3D lightToBrdf = brdfPosition - mediaPoint;
            float distSqr = Math.Max(lightToBrdf.SquareLength(), MagicNumbers.MinAllowedPathPointSqrDistance);
            return brdfPdfW / distSqr;
        }

        public static float PdfWToPdfAOrV(float pdfW, PathPoint brdfSamplePoint, PathPoint destinationPoint)
        {
            bool noSqrtDivision = brdfSamplePoint.IsLocatedOnInfinityAwayLightSource || destinationPoint.IsLocatedOnInfinityAwayLightSource;

            if (destinationPoint.LocationType == MediaPointLocationType.Surface || destinationPoint.LocationType == MediaPointLocationType.Camera ||
                destinationPoint.LocationType == MediaPointLocationType.MediaBorder)
            {
                if (noSqrtDivision) 
                    return PdfHelper.PdfWtoANoSqrtDivision(pdfW, brdfSamplePoint, destinationPoint);

                return PdfHelper.PdfWtoA(pdfW, brdfSamplePoint, destinationPoint);
            }
            else
            {
                if (noSqrtDivision) 
                    return pdfW;

                return PdfHelper.PdfWtoV(pdfW, brdfSamplePoint.Position, destinationPoint.Position);
            }
        }
    }
}

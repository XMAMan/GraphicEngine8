using GraphicMinimal;
using System;

namespace IntersectionTests.BeamLine
{
    public static class LineBeamIntersectionHelper
    {
        //Quelle: SmallUPBP PhotonBeam.hxx Zeile 238 static __forceinline bool testIntersectionBeamBeam(
        public static LineBeamIntersectionPoint GetLineBeamIntersectionPoint(IQueryLine line, IIntersectableCylinder beam)
        {
            Vector3D d1d2c = Vector3D.Cross(line.Ray.Direction, beam.Ray.Direction);
            float sinThetaSqr = d1d2c * d1d2c; // Square of the sine between the two lines (||cross(d1, d2)|| = sinTheta).

            //https://math.stackexchange.com/questions/2213165/find-shortest-distance-between-lines-in-3d -> Formel für Distanz zwischen zwei 3D-Linien
            // Slower code to test if the lines are too far apart.
            //float oDistance1 = Math.Abs((beam.Ray.Start - line.Ray.Start) * d1d2c) / d1d2c.Length();
            //if(oDistance1 * oDistance1 >= beam.RadiusSqrt) return null; 

            float ad = (beam.Ray.Start - line.Ray.Start) * d1d2c;

            // Lines too far apart.
            if (ad * ad >= beam.RadiusSqrt * sinThetaSqr)
                return null;

            // Cosine between the two lines.
            float d1d2 = line.Ray.Direction * beam.Ray.Direction;
            float d1d2Sqr = d1d2 * d1d2;
            float d1d2SqrMinus1 = d1d2Sqr - 1.0f;

            // Parallel lines?
            if (d1d2SqrMinus1 < 1e-5f && d1d2SqrMinus1 > -1e-5f)
                return null;

            float d1O1 = line.Ray.Direction * line.Ray.Start;
            float d1O2 = line.Ray.Direction * beam.Ray.Start;

            //https://en.wikipedia.org/wiki/Skew_lines#Distance -> Formel für oT1 und oT2
            //Vector3D n2 = Vector3D.Cross(beam.Ray.Direction, d1d2c);
            //float oT1_ = (beam.Ray.Start - line.Ray.Start) * n2 / (line.Ray.Direction * n2);            
            Vector3D n1 = Vector3D.Cross(line.Ray.Direction, -d1d2c);
            float oT2_ = (line.Ray.Start - beam.Ray.Start) * n1 / (beam.Ray.Direction * n1);

            float oT1 = (d1O1 - d1O2 - d1d2 * ((beam.Ray.Direction * line.Ray.Start) - (beam.Ray.Direction * beam.Ray.Start))) / d1d2SqrMinus1;

            // Out of range on ray 1.
            if (oT1 <= 0 || oT1 >= line.LongRayLength)
                return null;

            //float oT2 = (oT1 + d1O1 - d1O2) / d1d2; //Diese Formel/Zeile stammt aus SmallUPBP und ist falsch!!!
            float oT2 = oT2_;
            // Out of range on ray 2.
            if (oT2 <= 0 || oT2 >= beam.Length || float.IsNaN(oT2))
                return null;

            float sinTheta = (float)Math.Sqrt(sinThetaSqr);

            float oDistance = Math.Abs(ad) / sinTheta;

            float oSinTheta = sinTheta;

            // Found an intersection.
            return new LineBeamIntersectionPoint()
            {
                IntersectedBeam = beam,
                QueryLine = line,
                LineIntersectionPosition = oT1,
                BeamIntersectionPosition = oT2,
                Distance = oDistance,
                SinTheta = oSinTheta,
            };
        }
    }
}

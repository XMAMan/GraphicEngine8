using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class IntersectableTriangle : Triangle, IFlatIntersectableObject, IParseableString
    {
        public IIntersectableRayDrawingObject RayHeigh { get; protected set; } //Die Lichtquelle muss beim Schattenstrahltest wissen, ob Lichtquelle-RayHeigh == IntersectionPoint.RayHeigh
        public Vector3D AABBCenterPoint { get; private set; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        public Vector3D MinPoint { get; private set; }
        public Vector3D MaxPoint { get; private set; }

        //Frage lieber nicht nach, was diese Variablen alles bedeuten. Ich habe sie einfach von http://www.flipcode.com/archives/Raytracing_Topics_Techniques-Part_7_Kd-Trees_and_More_Speed.shtml übernommen
        protected float /*m_U, m_V,*/ nu, nv, nd, bnu, bnv, cnu, cnv;
        protected int k, ku, kv;

        public IntersectableTriangle(Triangle triangle, IIntersectableRayDrawingObject rayHeigh)
            : base(triangle)
        {
            this.RayHeigh = rayHeigh;
            Vector3D A = triangle.V[0].Position;
            Vector3D B = triangle.V[1].Position;
            Vector3D C = triangle.V[2].Position;
            Vector3D c = B - A;
            Vector3D b = C - A;
            Vector3D normal = Vector3D.Cross(b, c);
            int u, v;
            if (Math.Abs(normal.X) > Math.Abs(normal.Y))
            {
                if (Math.Abs(normal.X) > Math.Abs(normal.Z)) this.k = 0; else this.k = 2;
            }
            else
            {
                if (Math.Abs(normal.Y) > Math.Abs(normal.Z)) this.k = 1; else this.k = 2;
            }
            u = (this.k + 1) % 3;
            v = (this.k + 2) % 3;
            this.ku = u;
            this.kv = v;
            // precomp
            float krec = 1.0f / normal[this.k];
            this.nu = normal[u] * krec;
            this.nv = normal[v] * krec;
            this.nd = (normal * A) * krec;
            // first line equation
            float reci = 1.0f / (b[u] * c[v] - b[v] * c[u]);
            this.bnu = b[u] * reci;
            this.bnv = -b[v] * reci;
            // second line equation
            this.cnu = c[v] * reci;
            this.cnv = -c[u] * reci;

            Vector3D minPoint = new Vector3D(A);
            if (B.X < minPoint.X) minPoint.X = B.X;
            if (B.Y < minPoint.Y) minPoint.Y = B.Y;
            if (B.Z < minPoint.Z) minPoint.Z = B.Z;
            if (C.X < minPoint.X) minPoint.X = C.X;
            if (C.Y < minPoint.Y) minPoint.Y = C.Y;
            if (C.Z < minPoint.Z) minPoint.Z = C.Z;
            Vector3D maxPoint = new Vector3D(A);
            if (B.X > maxPoint.X) maxPoint.X = B.X;
            if (B.Y > maxPoint.Y) maxPoint.Y = B.Y;
            if (B.Z > maxPoint.Z) maxPoint.Z = B.Z;
            if (C.X > maxPoint.X) maxPoint.X = C.X;
            if (C.Y > maxPoint.Y) maxPoint.Y = C.Y;
            if (C.Z > maxPoint.Z) maxPoint.Z = C.Z;

            //Vector3D pa2 = Vector3D.Cross(C - B, B - A);

            this.MinPoint = minPoint;
            this.MaxPoint = maxPoint;
            this.AABBCenterPoint = this.MinPoint + (this.MaxPoint - this.MinPoint) / 2;
        }

        public new string ToCtorString()
        {
            return $"new IntersectableTriangle({base.ToCtorString()},null)";
        }

        public override string ToString()
        {
            return this.RayHeigh.Propertys.Name + " [" + this.V[0].Position.ToString() + "|" + this.V[1].Position.ToString() + "|" + this.V[2].Position.ToString() + "] IsLightSource=" + (this.RayHeigh.Propertys.RaytracingLightSource != null ? "true" : "false");
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            List<IIntersectionPointSimple> list = new List<IIntersectionPointSimple>();
            var point = GetSimpleIntersectionPoint(ray, time);
            if (point != null) list.Add(point);
            return list;
        }

        //Quelle: http://www.flipcode.com/archives/Raytracing_Topics_Techniques-Part_7_Kd-Trees_and_More_Speed.shtml
        //Das u und v, was hier berechent wird sind Bayzentrische Koordinaten. Das sind KEINE Texturkoordinaten!!!
        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            //Geometrischer Schnittpunkttest
            Vector3D O = ray.Start;
            Vector3D D = ray.Direction;
            Vector3D A = this.V[0].Position;
            float Ind = 1.0f / (D[this.k] + this.nu * D[this.ku] + this.nv * D[kv]);
            if (float.IsInfinity(Ind) || float.IsNaN(Ind)) return null;
            float t = (this.nd - O[this.k] - this.nu * O[this.ku] - this.nv * O[this.kv]) * Ind;
            if (t <= 0f) return null;
            float hu = O[this.ku] + t * D[this.ku] - A[this.ku];
            float hv = O[this.kv] + t * D[this.kv] - A[this.kv];
            float beta = hv * this.bnu + hu * this.bnv;
            if (beta < 0f) return null;
            float gamma = hu * this.cnu + hv * this.cnv;
            if (gamma < 0f) return null;

            //So darf ich es nicht machen da sonst IntersectableTriangleTest.GetSimpleIntersectionPoint_CalledForZRightSideRay_NoIntersectionFound rot wird
            //float sum = beta + gamma;
            //if (sum > 1f) return null; //Lösung von flipcode (Numberisch ungenau)
            float alpha = 1 - beta - gamma;
            if (alpha < 0) return null; //Lösung von XMAMan

            ParallaxPoint parallaxPoint = null;
            Vector3D position = ray.Start + ray.Direction * t;

            //Berechne ParallaxPoint, wenn nötig
            if (this.RayHeigh.Propertys.NormalSource.Type == NormalSource.Parallax && this.RayHeigh.Propertys.NormalSource.As<NormalFromParallax>().IsParallaxEdgeCutoffEnabled)
            {
                //Wenn ich beim vorderen Dreieck aufgrund der Parallax-Edge-Bedingung durchfliege, dann stelle sicher
                //das ich beim Nachbardreieck nicht von der Rückseite ein Schnittpunkt bekomme
                if (this.Normal * ray.Direction > 0) return null;

                //Texturkoordinaten
                float textcoordU = alpha * this.V[0].TexcoordU + beta * this.V[1].TexcoordU + gamma * this.V[2].TexcoordU;
                float textcoordV = alpha * this.V[0].TexcoordV + beta * this.V[1].TexcoordV + gamma * this.V[2].TexcoordV;

                //Shaded-Normale
                Vector3D shadedNormal;
                if (this.RayHeigh.Propertys.NormalInterpolation == InterpolationMode.Flat)
                {
                    //OrientedFlatNormal
                    Vector3D orientedFlatNormal = this.Normal;
                    if (orientedFlatNormal * ray.Direction > 0) orientedFlatNormal = -orientedFlatNormal;
                    shadedNormal = orientedFlatNormal;
                }
                else
                {
                    //Gouraud-Shading
                    shadedNormal = Vector3D.Normalize(alpha * this.V[0].Normal + beta * this.V[1].Normal + gamma * this.V[2].Normal);
                    if (shadedNormal * ray.Direction > 0) shadedNormal = -shadedNormal;
                }               

                Vertex interpolatedVertexWithoutParallax = new Vertex(position, shadedNormal, this.Tangent, textcoordU, textcoordV);

                parallaxPoint = this.RayHeigh.ParallaxMap.GetParallaxIntersectionPointFromOutToIn(interpolatedVertexWithoutParallax, ray.Direction);
                if (parallaxPoint == null) return null;
            }

            //Berechne Farbe, wenn nötig
            if (this.RayHeigh.Propertys.BlackIsTransparent)
            {
                float textcoordU = alpha * this.V[0].TexcoordU + beta * this.V[1].TexcoordU + gamma * this.V[2].TexcoordU;
                float textcoordV = alpha * this.V[0].TexcoordV + beta * this.V[1].TexcoordV + gamma * this.V[2].TexcoordV;
                if (this.RayHeigh.IsBlackColor(textcoordU, textcoordV, position)) return null;
            }

            return new TriangleIntersectionPoint(this, position, t, parallaxPoint, ray.Direction, alpha, beta, gamma);
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            TriangleIntersectionPoint point = (TriangleIntersectionPoint)simplePoint;

            //Texturkoordinaten
            float textcoordU = point.Alpha * this.V[0].TexcoordU + point.Beta * this.V[1].TexcoordU + point.Gamma * this.V[2].TexcoordU;
            float textcoordV = point.Alpha * this.V[0].TexcoordV + point.Beta * this.V[1].TexcoordV + point.Gamma * this.V[2].TexcoordV;

            //OrientedFlatNormal
            Vector3D orientedFlatNormal = this.Normal;
            if (orientedFlatNormal * point.RayDirection > 0) orientedFlatNormal = -orientedFlatNormal;

            //Shaded-Normale
            Vector3D shadeNormal;
            if (this.RayHeigh.Propertys.NormalInterpolation == InterpolationMode.Flat)
            {
                shadeNormal = orientedFlatNormal;
            }
            else
            {
                //Gouraud-Shading
                shadeNormal = point.Alpha * this.V[0].Normal + point.Beta * this.V[1].Normal + point.Gamma * this.V[2].Normal;
                float normalLength = shadeNormal.Length();
                if (normalLength == 0) shadeNormal = orientedFlatNormal; else shadeNormal /= normalLength;
                //shadeNormal = Vector3D.Normalize(f * this.V[0].Normal + point.Beta * this.V[1].Normal + point.Gamma * this.V[2].Normal); //So kann ich eine Division durch 0 erhalten
                if (shadeNormal * point.RayDirection > 0) shadeNormal = -shadeNormal;
            }

            return this.RayHeigh.CreateIntersectionPoint(
                new Vertex(point.Position, shadeNormal, this.Tangent, textcoordU, textcoordV),
                orientedFlatNormal, this.Normal, point.RayDirection, point.ParallaxPoint, this);

        }
    }
}

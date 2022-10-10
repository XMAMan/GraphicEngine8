using System;
using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using System.Drawing;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;

namespace RayObjects.RayObjects
{
    public class RayTriangle : IntersectableTriangle, IFlatIntersectableObject, IUniformRandomSurfacePointCreator, IUVMapable, IRayObject, IFlatObject, IFlatRandomPointCreator
    {
        private readonly Vector3D edgePos1;
        private readonly Vector3D edgePos2;
        private readonly Vector2D edgeTex1;
        private readonly Vector2D edgeTex2;
        private readonly Plane trianglePlane;

        public RayTriangle(Triangle triangle, IIntersectableRayDrawingObject rayHigh)
            : base(triangle, rayHigh)
        {
            this.edgePos1 = triangle.V[1].Position - triangle.V[0].Position;
            this.edgePos2 = triangle.V[2].Position - triangle.V[0].Position;
            this.edgeTex1 = triangle.V[1].TextcoordVector - triangle.V[0].TextcoordVector;
            this.edgeTex2 = triangle.V[2].TextcoordVector - triangle.V[0].TextcoordVector;

            this.trianglePlane = triangle.GetTrianglePlane();

            this.CenterOfGravity = (this.V[0].Position + this.V[1].Position + this.V[2].Position) / 3;
        }

        //Kopierkonstruktor
        public RayTriangle(RayTriangle copy)
            :this(copy, copy.RayHeigh)
        {
        }

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            // get two randoms
            float sqr1 = (float)Math.Sqrt(rand.NextDouble());
            float r2 = (float)rand.NextDouble();

            // make barycentric coords
            float a = 1.0f - sqr1;
            float b = (1.0f - r2) * sqr1;

            // make position from barycentrics
            // calculate interpolation by using two edges as axes scaled by the
            // barycentrics
            Vector3D position = this.edgePos1 * a + this.edgePos2 * b + this.V[0].Position;
            Vector2D texcoord = this.edgeTex1 * a + this.edgeTex2 * b + this.V[0].TextcoordVector;
            Vector3D color = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, position);
            return new SurfacePoint(position, this.Normal, color, this, 1.0f / this.SurfaceArea);
        }

        //https://math.stackexchange.com/questions/18686/uniform-random-point-in-triangle
        //http://www.cs.princeton.edu/~funk/tog02.pdf Section 4.2
        public SurfacePoint GetSurfacePointFromUAndV(double u1, double u2)
        {
            // get two randoms
            float sqr1 = (float)Math.Sqrt(u1);
            float r2 = (float)u2;

            // make barycentric coords
            float a = 1.0f - sqr1;
            float b = (1.0f - r2) * sqr1;
            float c = r2 * sqr1;

            /*if (u1+u2 <= 1)
            {
                a = (float)u1;
                b = (float)u2;
                c = (float)(1 - u1 - u2);
            }else
            {
                u1 = 1 - u1;
                u2 = 1 - u2;
                a = (float)u1;
                b = (float)u2;
                c = (float)(1 - u1 - u2);
            }*/

            // make position from barycentrics
            // calculate interpolation by using two edges as axes scaled by the
            // barycentrics
            Vector3D position = this.V[0].Position * a + this.V[1].Position * b + this.V[2].Position * c;
            Vector2D texcoord = this.V[0].TextcoordVector * a + this.V[1].TextcoordVector * b + this.V[2].TextcoordVector * c;
            Vector3D color = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, position);
            return new SurfacePoint(position, this.Normal, color, this, 1.0f / this.SurfaceArea);
        }
        public void GetUAndVFromSurfacePoint(Vector3D position, out double u1, out double u2)
        {
			//https://computergraphics.stackexchange.com/questions/1866/how-to-map-square-texture-to-triangle
            Vector3D A = this.V[0].Position;
            float hu = position[this.ku] - A[this.ku];
            float hv = position[this.kv] - A[this.kv];

            double beta = hv * this.bnu + hu * this.bnv; //b
            double gamma = hu * this.cnu + hv * this.cnv; //c
            //double f = 1 - beta - gamma; //a

            if (beta < 0f) { u1 = u2 = double.NaN; return; }
            if (gamma < 0f) { u1 = u2 = double.NaN; return; }
            double sum = beta + gamma;
            if (sum > 1f) { u1 = u2 = double.NaN; return; }

            u1 = (beta + gamma) * (beta + gamma);
            u2 = gamma / (beta + gamma);
        }
        public double GetSurfaceAreaFromUVRectangle(RectangleF uvRectangle)
        {
            Vector3D tangent = Vector3D.Normalize(this.edgePos1);
            Vector3D binormal = Vector3D.Cross(this.Normal, tangent);
            Frame frame = new Frame(tangent, binormal, this.Normal);
            
            Vector3D p1 = GetSurfacePointFromUAndV(uvRectangle.X, uvRectangle.Y).Position;
            Vector3D p2 = GetSurfacePointFromUAndV(uvRectangle.X + uvRectangle.Width, uvRectangle.Y).Position;
            Vector3D p3 = GetSurfacePointFromUAndV(uvRectangle.X + uvRectangle.Width, uvRectangle.Y + uvRectangle.Height).Position;
            Vector3D p4 = GetSurfacePointFromUAndV(uvRectangle.X, uvRectangle.Y + uvRectangle.Height).Position;

            Vector3D d1 = frame.ToLocal(p1 - this.V[0].Position);
            Vector3D d2 = frame.ToLocal(p2 - this.V[0].Position);
            Vector3D d3 = frame.ToLocal(p3 - this.V[0].Position);
            Vector3D d4 = frame.ToLocal(p4 - this.V[0].Position);
            Polygon polygon = new Polygon(new List<Vector2D>()
            {
                new Vector2D(d1.X, d1.Y),
                new Vector2D(d2.X, d2.Y),
                new Vector2D(d3.X, d3.Y),
                new Vector2D(d4.X, d4.Y),
            });

            return polygon.GetSurfaceArea();
        }


        public bool IsPointAbovePlane(Vector3D point)
        {
            return this.trianglePlane.IsPointAbovePlane(point);
        }
        public Vector3D CenterOfGravity { get; private set; } //Schwerpunkt

        public override IDivideable[] Divide()
        {
            return base.Divide().Select(x => new RayTriangle(x as Triangle, this.RayHeigh)).ToArray();
        }
    }
}

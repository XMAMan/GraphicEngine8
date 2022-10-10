using System.Collections.Generic;
using System.Linq;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;
using System.Drawing;

namespace RayObjects.RayObjects
{
    public class RayQuad : IntersectableQuad, IFlatIntersectableObject, IUniformRandomSurfacePointCreator, IRayObject, IFlatObject, IFlatRandomPointCreator, IUVMapable
    {
       
        public RayQuad(Quad quad, IIntersectableRayDrawingObject rayHigh)
            : base(quad, rayHigh)
        {
            this.CenterOfGravity = (quad.v1.Position + quad.v2.Position + quad.v3.Position + quad.v4.Position) / 4;
            
        }

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            float r1 = (float)rand.NextDouble(), r2 = (float)rand.NextDouble();
            Vector3D position = this.edgePos1 * r1 + this.edgePos2 * r2 + this.v1.Position;
            Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, r1, r2);
            Vector3D color = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, position);
            return new SurfacePoint(position, this.Normal, color, this, 1.0f / this.SurfaceArea);
        }

        public Vector3D CenterOfGravity { get; private set; } //Schwerpunkt
        public bool IsPointAbovePlane(Vector3D point)
        {
            return this.plane.IsPointAbovePlane(point);
        }

        public override IDivideable[] Divide()
        {
            return base.Divide().Select(x => new RayQuad(x as Quad, this.RayHeigh)).ToArray();
        }

        public List<RayTriangle> DivideIntoTwoTriangles()
        {
            return new List<RayTriangle>()
            {
                //new RayTriangle(new Triangle(this.v1, this.v2, this.v3), this.RayHeigh),
                //new RayTriangle(new Triangle(this.v3, this.v4, this.v1), this.RayHeigh),
                new RayTriangle(new Triangle(this.v2, this.v3, this.v4), this.RayHeigh),
                new RayTriangle(new Triangle(this.v4, this.v1, this.v2), this.RayHeigh),
            };
        }

        public SurfacePoint GetSurfacePointFromUAndV(double u1, double u2)
        {
            float r1 = (float)u1, r2 = (float)u2;
            Vector3D position = this.edgePos1 * r1 + this.edgePos2 * r2 + this.v1.Position;
            Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, r1, r2);
            Vector3D color = this.RayHeigh.GetColor(texcoord.X, texcoord.Y, position);
            return new SurfacePoint(position, this.Normal, color, this, 1.0f / this.SurfaceArea);
        }

        public void GetUAndVFromSurfacePoint(Vector3D position, out double u1, out double u2)
        {
            Vector3D worldDirection = position - this.v1.Position;

            u1 = (worldDirection * Vector3D.Normalize(this.edgePos1)) / this.edgeLength1;
            u2 = (worldDirection * Vector3D.Normalize(this.edgePos2)) / this.edgeLength2;
        }

        public double GetSurfaceAreaFromUVRectangle(RectangleF uvRectangle)
        {
            return uvRectangle.Width * this.edgeLength1 * uvRectangle.Height * this.edgeLength2;
        }
    }
}

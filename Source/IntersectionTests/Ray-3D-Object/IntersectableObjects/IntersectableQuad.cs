using System;
using System.Collections.Generic;
using GraphicMinimal;
using GraphicGlobal;

namespace IntersectionTests.Ray_3D_Object.IntersectableObjects
{
    public class IntersectableQuad : Quad, IFlatIntersectableObject, IParseableString
    {
        public IIntersectableRayDrawingObject RayHeigh { get; protected set; } //Die Lichtquelle muss beim Schattenstrahltest wissen, ob Lichtquelle-RayHeigh == IntersectionPoint.RayHeigh
        public Vector3D AABBCenterPoint { get; private set; } //Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        public Vector3D MinPoint { get; private set; }
        public Vector3D MaxPoint { get; private set; }

        protected Plane plane;
        protected float edgeLength1;
        protected float edgeLength2;

        public IntersectableQuad(Quad quad, IIntersectableRayDrawingObject rayHeigh)
            :base(quad)
        {
            this.RayHeigh = rayHeigh;
            this.MinPoint = new Vector3D(Math.Min(Math.Min(Math.Min(quad.v1.Position.X, quad.v2.Position.X), quad.v3.Position.X), quad.v4.Position.X), Math.Min(Math.Min(Math.Min(quad.v1.Position.Y, quad.v2.Position.Y), quad.v3.Position.Y), quad.v4.Position.Y), Math.Min(Math.Min(Math.Min(quad.v1.Position.Z, quad.v2.Position.Z), quad.v3.Position.Z), quad.v4.Position.Z));
            this.MaxPoint = new Vector3D(Math.Max(Math.Max(Math.Max(quad.v1.Position.X, quad.v2.Position.X), quad.v3.Position.X), quad.v4.Position.X), Math.Max(Math.Max(Math.Max(quad.v1.Position.Y, quad.v2.Position.Y), quad.v3.Position.Y), quad.v4.Position.Y), Math.Max(Math.Max(Math.Max(quad.v1.Position.Z, quad.v2.Position.Z), quad.v3.Position.Z), quad.v4.Position.Z));
            this.AABBCenterPoint = this.MinPoint + (this.MaxPoint - this.MinPoint) / 2;
            this.plane = new Plane(quad.Normal, quad.v1.Position);
            this.edgeLength1 = quad.edgePos1.Length();
            this.edgeLength2 = quad.edgePos2.Length();
        }

        public new string ToCtorString()
        {
            return $"new IntersectableQuad({base.ToCtorString()},null)";
        }

        public override string ToString()
        {
            return this.RayHeigh.Propertys.Name + " [" + this.v1.Position.ToString() + "|" + this.v2.Position.ToString() + "|" + this.v3.Position.ToString() + "|" + this.v4.Position.ToString() + "] IsLightSource=" + (this.RayHeigh.Propertys.RaytracingLightSource != null ? "true" : "false");
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            List<IIntersectionPointSimple> list = new List<IIntersectionPointSimple>();
            var point = GetSimpleIntersectionPoint(ray, time);
            if (point != null) list.Add(point);
            return list;
        }

        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            Vector3D pointOnPlane = this.plane.GetIntersectionPointWithRay(ray, out float distance);
            if (pointOnPlane == null) return null;
            Vector3D worldDirection = pointOnPlane - this.v1.Position;

            float fx = (worldDirection * Vector3D.Normalize(this.edgePos1)) / this.edgeLength1;
            float fy = (worldDirection * Vector3D.Normalize(this.edgePos2)) / this.edgeLength2;

            if (fx >= 0 && fx <= 1 && fy >= 0 && fy <= 1 && distance > 0)
            {
                ParallaxPoint parallaxPoint = null;
                Vector3D position = ray.Start + ray.Direction * distance;

                //Berechne VertexPoint, wenn nötig
                if (this.RayHeigh.Propertys.NormalSource.Type == NormalSource.Parallax && this.RayHeigh.Propertys.NormalSource.As<NormalFromParallax>().IsParallaxEdgeCutoffEnabled)
                {
                    Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, fx, fy);

                    //Shaded-Normale
                    Vector3D shadedNormal = null;
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
                        shadedNormal = Vector3D.Normalize(InterpolateVectorOverQuad(this.v1.Normal, this.v2.Normal, this.v3.Normal, this.v4.Normal, fx, fy));
                        if (shadedNormal * ray.Direction > 0) shadedNormal = -shadedNormal;
                    }

                    Vector3D tangent = this.Tangent;

                    Vertex interpolatedVertexWithoutParallax = new Vertex(position, shadedNormal, tangent, texcoord.X, texcoord.Y);

                    parallaxPoint = this.RayHeigh.ParallaxMap.GetParallaxIntersectionPointFromOutToIn(interpolatedVertexWithoutParallax, ray.Direction);
                    if (parallaxPoint == null) return null;
                }

                //Berechne Farbe, wenn nötig
                if (this.RayHeigh.Propertys.BlackIsTransparent)
                {
                    Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, fx, fy);
                    if (this.RayHeigh.IsBlackColor(texcoord.X, texcoord.Y, position)) return null;
                }

                return new QuadIntersectionPoint(this, position, distance, parallaxPoint, ray.Direction, fx, fy);
            }

            return null;
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            QuadIntersectionPoint point = (QuadIntersectionPoint)simplePoint;

            Vector2D texcoord = InterpolateVector2DOverQuad(this.v1.TextcoordVector, this.v2.TextcoordVector, this.v3.TextcoordVector, this.v4.TextcoordVector, point.F1, point.F2);

            //OrientedFlatNormal
            Vector3D orientedFlatNormal = this.Normal;
            if (orientedFlatNormal * point.RayDirection > 0) orientedFlatNormal = -orientedFlatNormal;

            //Shaded-Normale
            Vector3D shadeNormal = null;
            if (this.RayHeigh.Propertys.NormalInterpolation == InterpolationMode.Flat)
            {
                shadeNormal = orientedFlatNormal;
            }
            else
            {
                //Gouraud-Shading
                shadeNormal = Vector3D.Normalize(InterpolateVectorOverQuad(this.v1.Normal, this.v2.Normal, this.v3.Normal, this.v4.Normal, point.F1, point.F2));
                if (shadeNormal * point.RayDirection > 0) shadeNormal = -shadeNormal;
            }

            Vector3D tangent = this.Tangent;

            return this.RayHeigh.CreateIntersectionPoint(
                new Vertex(point.Position, shadeNormal, tangent, texcoord.X, texcoord.Y),
                orientedFlatNormal, this.Normal, point.RayDirection, point.ParallaxPoint, this);
        }

        //3  2
        //0  1  
        protected Vector3D InterpolateVectorOverQuad(Vector3D v0, Vector3D v1, Vector3D v2, Vector3D v3, float fx, float fy)
        {
            //Vector3D position = this.edge1 * fx + this.edge2 * fy + this.v1.Position; //Nur Positionsvektoren darf man so interpolieren. Der Rest muss über alle 4 Kanten gemacht werden

            Vector3D xEdge1 = v1 - v0;
            Vector3D xEdge2 = v2 - v3;

            Vector3D X1 = v0 + xEdge1 * fx;
            Vector3D X2 = v3 + xEdge2 * fx;
            Vector3D yEdge = X2 - X1;
            Vector3D p = X1 + yEdge * fy;

            return p;
        }

        //3  2
        //0  1  
        protected Vector2D InterpolateVector2DOverQuad(Vector2D v0, Vector2D v1, Vector2D v2, Vector2D v3, float fx, float fy)
        {
            //Vector3D position = this.edge1 * fx + this.edge2 * fy + this.v1.Position; //Nur Positionsvektoren darf man so interpolieren. Der Rest muss über alle 4 Kanten gemacht werden

            Vector2D xEdge1 = v1 - v0;
            Vector2D xEdge2 = v2 - v3;

            Vector2D X1 = v0 + xEdge1 * fx;
            Vector2D X2 = v3 + xEdge2 * fx;
            Vector2D yEdge = X2 - X1;
            Vector2D p = X1 + yEdge * fy;

            return p;
        }
    }
}

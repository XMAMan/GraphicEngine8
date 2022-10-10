using System;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using IntersectionTests.Ray_3D_Object.IntersectableObjects;

namespace RayObjects.RayObjects
{
    public class RaySphere : IntersectableSphere, IIntersecableObject, IRayObject
    {
        public RaySphere(Vector3D center, float radius, IIntersectableRayDrawingObject rayHigh)
            : base(center, radius, rayHigh)
        {
            this.SurfaceArea = 4.0f * this.Radius * this.Radius * (float)Math.PI;
        }        

        public SurfacePoint GetRandomPointOnSurface(IRandom rand)
        {
            Vector3D position = this.Center + Vector3D.GetRandomDirection(rand.NextDouble(), rand.NextDouble()) * this.Radius;

            Vector3D normal = Vector3D.Normalize(position - this.Center);
            Vector3D texNormal = Vector3D.Normalize(Matrix4x4.MultDirection(this.inverseNormalMatrix, normal));

            //http://www.scratchapixel.com/code.php?id=34&origin=/lessons/3d-basic-rendering/global-illumination-path-tracing
            float textcoordV = (float)((1 - Math.Atan2(texNormal.Z, texNormal.X) / Math.PI) * 0.5);
            float textcoordU = (float)(Math.Acos(texNormal.Y) / Math.PI);
            Vector3D color = this.RayHeigh.GetColor(textcoordU, textcoordV, position);
            return new SurfacePoint(position, normal, color, this, 1.0f / this.SurfaceArea);
        }

        public float SurfaceArea { get; private set; }
    }
}

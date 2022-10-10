using System;
using System.Collections.Generic;
using IntersectionTests;
using GraphicMinimal;
using GraphicGlobal;
using SubpathGenerator;

namespace Photonusmap
{
    class VolumetricPhoton : IIntersecableObject, ISphere
    {
        public IIntersectableRayDrawingObject RayHeigh { get { return null; } }

        private float radius = 0;
        public float Radius { get { return this.radius; } set { this.radius = value; this.RadiusSquared = value * value; } }
        private float RadiusSquared = 1;

        public Vector3D AABBCenterPoint//Das ist der Mittelpunkt von der Axis Aligned Bounding Box
        {
            get
            {
                return this.lightPoint.Position;
            }
        }
        public Vector3D MinPoint 
        {
            get
            {
                return this.lightPoint.Position - rad * this.Radius;
            }
        }
        public Vector3D MaxPoint
        {
            get
            {
                return this.lightPoint.Position + rad * this.Radius;
            }
        }

        private readonly PathPoint lightPoint;
        
        public VolumetricPhoton(PathPoint lightPoint)
        {
            this.lightPoint = lightPoint;         
        }

        private readonly Vector3D rad = Vector3D.Normalize(new Vector3D(1, 1, 1));
        
        //SmallUPBP - Bre.cxx Zeile 199
        public IIntersectionPointSimple GetSimpleIntersectionPoint(Ray ray, float time)
        {
            Vector3D rayOrigToPhoton = this.lightPoint.Position - ray.Start;
            float isectDist = rayOrigToPhoton * ray.Direction;
            if (isectDist > 0)
            {
                Vector3D isecPosition = ray.Start + ray.Direction * isectDist;
                float isectRadSqr = (isecPosition - this.lightPoint.Position).SquareLength();
                if (isectRadSqr < this.RadiusSquared)
                {
                    return new PhotonIntersectionPoint(isecPosition, isectDist, this, isectRadSqr, this.lightPoint);
                }
            }

            return null; // Kein Schnittpunkt
        }

        public List<IIntersectionPointSimple> GetAllIntersectionPoints(Ray ray, float time)
        {
            List<IIntersectionPointSimple> list = new List<IIntersectionPointSimple>();
            var point = GetSimpleIntersectionPoint(ray, time);
            if (point != null) list.Add(point);
            return list;
        }

        public IntersectionPoint TransformSimplePointToIntersectionPoint(IIntersectionPointSimple simplePoint)
        {
            throw new NotImplementedException();
        }

        public float this[int d]
        {
            get
            {
                return this.lightPoint[d];
            }
        }
    }
}

using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;
using IntersectionTests.BeamLine;
using SubpathGenerator;
using System;

namespace Photonusmap
{
    public class BeamRay : IIntersectableCylinder
    {
        public Ray Ray { get; private set; } //Startpunkt + Richtung des Cylinders
        public float Length { get; private set; }
        private float radius = float.NaN;
        public float Radius
        {
            get
            {
                return this.radius;
            }
            set
            {
                this.radius = value;
                this.RadiusSqrt = value * value;
            }
        }
        public float RadiusSqrt { get; private set; }

        public BoundingBox GetAxialAlignedBoundingBox()
        {
            return GetNonAlignedBoundingBox().GetAxialAlignedBoundingBox();
        }
        public NonAlignedBoundingBox GetNonAlignedBoundingBox()
        {
            Vector3D w = this.Ray.Direction,
                   u = Vector3D.Normalize(Vector3D.Cross((Math.Abs(w.X) > 0.1f ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0)), w)),
                   v = Vector3D.Cross(w, u);

            float r = this.Radius;

            return new NonAlignedBoundingBox(this.Ray.Start - u * r - v * r, u * r * 2, v * r * 2, this.Ray.Direction * this.Length);
        }
        public LineBeamIntersectionPoint GetIntersectionPoint(IQueryLine line)
        {
            return LineBeamIntersectionHelper.GetLineBeamIntersectionPoint(line, this);
        }

        public BeamRay(PathPoint mediaLineStartPoint, MediaLine mediaLine, float radius)
        {
            this.LightPoint = mediaLineStartPoint;
            this.MediaLine = mediaLine;
            this.Ray = mediaLine.Ray;
            this.Length = mediaLine.LongRayLength;
            this.Radius = radius;
        }

        public PathPoint LightPoint { get; private set; } //Von diesen Punkt aus startet die MediaLine
        public MediaLine MediaLine { get; private set; }
    }
}

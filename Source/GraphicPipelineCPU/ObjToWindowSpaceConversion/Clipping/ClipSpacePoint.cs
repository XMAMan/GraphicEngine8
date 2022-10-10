using GraphicMinimal;
using GraphicPipelineCPU.DrawingHelper.Helper3D;
using System;

namespace GraphicPipelineCPU.ObjToWindowSpaceConversion.Clipping
{
    enum Axis
    {
        X,
        Y,
        Z,
        W
    }

    class ClipSpacePoint
    {
        public Interpolationvariables Interpolationvariables;               // Vertex ist in Eye-Koordinaten
        public Vector4D Position;   // Position ist die Vertexposition in Homogenen Koordinaten

        public float W
        {
            get
            {
                return this.Position.W;
            }
        }

        public float GetAxis(Axis axis, float signFromAxis)
        {
            switch (axis)
            {
                case Axis.X: return this.Position.X * signFromAxis;
                case Axis.Y: return this.Position.Y * signFromAxis;
                case Axis.Z: return this.Position.Z * signFromAxis;
                case Axis.W: return this.Position.W * signFromAxis;
            }
            throw new Exception("Wrong value for axis " + axis);
        }

        public ClipSpacePoint(Interpolationvariables interpolationvariables, Vector4D position)
        {
            this.Interpolationvariables = interpolationvariables;
            this.Position = position;
        }
    }
}

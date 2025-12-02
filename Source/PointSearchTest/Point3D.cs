using GraphicMinimal;
using RayTracerGlobal;

namespace PointSearchTest
{
    class Point3D : Vector3D, IPoint
    {
        //public float this[int dimension] { get { return this[dimension]; } }

        public Point3D(float x, float y, float z)
            :base(x,y,z)
        { }
    }
}

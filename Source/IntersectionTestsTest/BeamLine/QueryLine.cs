using GraphicGlobal;
using GraphicMinimal;
using IntersectionTests;

namespace IntersectionTestsTest.BeamLine
{
    class QueryLine : IQueryLine
    {
        public Ray Ray { get; private set; }
        public float LongRayLength { get; private set; }

        public QueryLine(Ray ray)
        {
            this.Ray = ray;
            this.LongRayLength = 2;
        }

        public QueryLine(Ray ray, float length)
        {
            this.Ray = ray;
            this.LongRayLength = length;
        }

        public QueryLine(float z, float startX, float startY, float endX, float endY)
        {
            Vector3D start = new Vector3D(startX, startY, z);
            Vector3D end = new Vector3D(endX, endY, z);
            this.Ray = new Ray(start, Vector3D.Normalize(end - start));
            this.LongRayLength = (end - start).Length();
        }
    }
}

using GraphicMinimal;

namespace GraphicGlobal
{
    public class Ray : IParseableString
    {
        public Vector3D Start;
        public Vector3D Direction;

        public Ray(Vector3D start, Vector3D direction)
        {
            this.Start = start;
            this.Direction = direction;
        }

        public string ToCtorString()
        {
            return $"new Ray({Start.ToCtorString()},{Direction.ToCtorString()})";
        }
    }
}

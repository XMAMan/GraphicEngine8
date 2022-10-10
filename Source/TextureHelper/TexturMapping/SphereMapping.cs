using GraphicMinimal;
using System;

namespace TextureHelper.TexturMapping
{
    public class SphereMapping : ITextureMapping
    {
        private Vector3D center;
        public SphereMapping(Vector3D center)
        {
            this.center = center;
        }
        public Vector2D Map(Vector3D pos)
        {
            Vector3D direction = Vector3D.Normalize(pos - this.center);

            float textcoordV = (float)((1 + Math.Atan2(direction.Z, direction.X) / Math.PI) * 0.5);
            float textcoordU = (float)(Math.Acos(direction.Y) / Math.PI);

            return new Vector2D(textcoordU, textcoordV);
        }
    }
}

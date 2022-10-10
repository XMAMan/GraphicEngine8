using GraphicGlobal;
using GraphicMinimal;
using System;

namespace TextureHelper.TexturMapping
{
    public class CylinderMapping : ITextureMapping
    {
        private Vector3D framePos;
        private Frame frame;
        private float radius;

        //cylinderRay.Start = Beliebiger Punkt auf dem Zylinder-Strahl
        //cylinderRay.Direction = Richtung des Zylinder-Strahls
        public CylinderMapping(Ray cylinderRay, float radius)
        {
            this.framePos = cylinderRay.Start;
            this.frame = new Frame(cylinderRay.Direction);
            this.radius = radius;
        }

        public Vector2D Map(Vector3D pos)
        {
            Vector3D posLocal = this.frame.ToLocal(pos - this.framePos);
            Vector3D pointOnRay = Vector3D.Projektion(posLocal, new Vector3D(0, 0, 1));
            Vector2D r  = (posLocal - pointOnRay).XY;

            double phi = Math.Atan2(r.Y, r.X);
            if (phi < 0) phi += 2 * Math.PI; //Bin ich im 3. oder 4. Quadrant? Dann rechne so um, dass ich Zahlen von 0 bis 2PI erhalte

            float u = Math.Min(1, r.Length() / this.radius);
            float v = (float)(phi / (2 * Math.PI));

            return new Vector2D(u, v);
        }
    }
}

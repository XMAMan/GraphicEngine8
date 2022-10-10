using GraphicMinimal;
using System;

namespace GraphicGlobal
{
    public class SphericalCoordinateConverter
    {
        private readonly Frame frame;

        //Basis für das lokale Koordinatensystem
        public Vector3D Normal { get { return this.frame.Normal; } }
        public Vector3D Tangent { get { return this.frame.Tangent; } }
        public Vector3D Binormal { get { return this.frame.Binormal; } }

        public SphericalCoordinateConverter()
        {
            this.frame = new Frame(new Vector3D(1, 0, 0), new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
        }

        public SphericalCoordinateConverter(Frame frame)
        {
            this.frame = frame;
        }

        public Vector3D ToWorldDirection(SphericalCoordinate sphereCoord)
        {
            float sinTheta = (float)Math.Sin(sphereCoord.Theta);
            Vector3D d = Vector3D.Normalize(((-this.frame.Tangent) * (float)Math.Cos(sphereCoord.Phi) * sinTheta + this.frame.Binormal * (float)Math.Sin(sphereCoord.Phi) * sinTheta + this.frame.Normal * (float)Math.Cos(sphereCoord.Theta)));
            return d;
        }

        //Returnwert: Phi = 0..2PI; Theta=0..PI
        public SphericalCoordinate ToSphereCoordinate(Vector3D worldDirection)
        {
            Vector3D localDirection = this.frame.ToLocal(worldDirection);

            SphericalCoordinate spherical = new SphericalCoordinate(0, 0);
            if (localDirection.Z < 0.999999f)
            {
                spherical.Theta = Math.Acos(localDirection.Z);

                //https://de.wikipedia.org/wiki/Arctan2
                //https://docs.microsoft.com/de-de/dotnet/api/system.math.atan2?view=netcore-3.1
                //Man muss bei Atan2 ein 2D-Richtungsvektor eingeben. Liegt der Richtungsvektor im 1 oder 2 Quadrant, gibt
                //Atan2 eine Phi-Zahl von 0 bis PI zurück.
                //Liegt der Richtungsvektor im 3. oder 4. Quadrant, dann gibt Atan2 eine Phi-Zahl von  -0 bis -PI zurück
                double phi = Math.Atan2(localDirection.Y, localDirection.X);
                phi = Math.PI - phi; //Auf diese Weise sorgt SmallUPBP dafür, dass phi immer im Bereich von 0 bis 2 PI liegt
                spherical.Phi = phi;
            }

            return spherical;
        }
    }
}

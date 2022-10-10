using GraphicMinimal;
using System.Drawing;

namespace TextureHelper
{
    public class TextureHelper
    {
        public static Vector3D TransformBumpNormalFromTangentToWorldSpace(Color bumpNormalSample, Matrix4x4 tangentToEyeMatrix)
        {
            Vector3D bumpNormal = TransformBumpColorToVector(bumpNormalSample);
            if (bumpNormal.Z < 0)  bumpNormal.Z = 0;
            return Vector3D.Normalize(Matrix4x4.MultDirection(tangentToEyeMatrix, bumpNormal));
        }

        private static Vector3D TransformBumpColorToVector(Color tangentSpaceBumpColor)
        {
            Vector3D bumpNormal = new Vector3D(tangentSpaceBumpColor.R / 255.0f, tangentSpaceBumpColor.G / 255.0f, tangentSpaceBumpColor.B / 255.0f);
            bumpNormal = (bumpNormal - new Vector3D(0.5f, 0.5f, 0.5f)) * 2.0f;//Expand the bump-map into a normalized signed vector(Bumpmap im Tangensspace)
            return bumpNormal;
        }

        //Wenn ich eine procedural erzeugte Normalmap abspeichern wöllte, dann wäre diese Funktion gut
        public static Color TransformBumpVectorToColor(Vector3D tangentSpaceNormal)
        {
            tangentSpaceNormal = (tangentSpaceNormal / 2.0f + new Vector3D(0.5f, 0.5f, 0.5f)) * 255;
            Color C = Color.FromArgb((int)tangentSpaceNormal.X, (int)tangentSpaceNormal.Y, (int)tangentSpaceNormal.Z);
            return C;
        }

    }
}

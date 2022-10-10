namespace GraphicMinimal
{
    public interface IMotionBlurMovement
    {
        AffineMatrizes GetMotionMatrizes(float time);
    }

    public class AffineMatrizes
    {
        public Matrix4x4 ObjectToWorldMatrix;
        public Matrix4x4 NormalObjectToWorldMatrix;
        public Matrix4x4 WorldToObjectMatrix;
    }
}

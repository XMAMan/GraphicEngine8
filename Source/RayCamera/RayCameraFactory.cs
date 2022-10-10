using GraphicMinimal;

namespace RayCameraNamespace
{
    public class CameraConstructorData
    {
        public Camera Camera;
        public int ScreenWidth;
        public int ScreenHeight;
        public ImagePixelRange PixelRange;
        public float DistanceDephtOfFieldPlane = 1;
        public float WidthDephtOfField = 1;
        public bool DepthOfFieldIsEnabled = false;
        public PixelSamplingMode SamplingMode;
        public bool UseCosAtCamera = true;
    }

    public static class RayCameraFactory
    {
        public static IRayCamera CreateCamera(CameraConstructorData data)
        {
            if (data.Camera.OpeningAngleY == 360) 
                return new SphereCamera(data);

            return new RayCamera(data);
        }
    }
}

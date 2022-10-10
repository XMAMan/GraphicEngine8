using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;


namespace RaytracingColorEstimator
{
    //Berechnet mit Hilfe eines Rand-Objektes einen Farbschätzwert für ein Pixel/Menge von Pixeln
    public interface IPixelEstimator
    {
        bool CreatesLigthPaths { get; } //Wird das FullPathSampleResult von GetFullPathSampleResult jemals LightPaths enthalten?
        void BuildUp(RaytracingFrame3DData data); //Erzeugt die RayObjekte/Einmalige Photonmaps/FullpathSampler
        FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand);
    }

    public interface IFrameEstimator : IPixelEstimator
    {
        void DoFramePrepareStep(ImagePixelRange range, int frameIterationNumber, IRandom rand);
        IFrameEstimator CreateCopy(); //Damit können viele Threads hintereinander ein und die selben PixelPhotonenCounter/FrameIterationCounter-Daten nutzen
    }
}

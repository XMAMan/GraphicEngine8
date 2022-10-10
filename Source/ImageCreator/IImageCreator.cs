using GraphicMinimal;
using System;
using System.Threading;

namespace ImageCreator
{
    //Berechnet mit mehreren Threads ein einzelnes SaveFileKästchen
    public interface IImageCreator
    {
        //range Bereich innerhalb von RaytracingFrame3DData.PixelRange (Immer im Bezug auf die linke obere Fensterecke (0,0))
        ImageBuffer GetImage(ImagePixelRange range); 
        float Progress { get; } //Zahl zwischen 0 und 1; Gibt an zu wie viel Prozent GetImage fertig ist
        string ProgressText { get; }
        ImageBuffer GetProgressImage();
        Vector3D GetColorFromSinglePixelForDebuggingPurpose(RaytracingDebuggingData debuggingData);
    }

    public interface IImageCreatorPixel : IImageCreator
    {
        ImageBuffer GetImageFromInitialData(ImageBuffer initialData, ImagePixelRange range);
    }

    public interface IImageCreatorFrame : IImageCreator
    {
        ImageBuffer GetImageFromInitialData(ImageBufferSum initialData, ImagePixelRange range);
        ImageBufferSum GetImageBufferSum(); //Wird zum Speichern benötigt
    }

    public enum RaytracerWorkingState 
    { 
        Created,
        LoadingAfterResume,
        InWork,
        Finish
    }
    public interface IImageCreatorWithSave : IImageCreator
    {
        RaytracerWorkingState State { get; }
        void SaveToFolder();    //Speichert das aktuell in Bearbeitung befindlich SaveArea-Kästchen in den Sicherungsordner
    }

    public interface IMasterImageCreator : IImageCreatorWithSave
    {
        void StopRaytracing();
    }
}

using GraphicGlobal;
using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RaytracingColorEstimator
{
    //Enthält all die Daten, die der Raytracing-Algorithmus als Input benötigt als auch den Stoptriggerund ProgresChanged
    //um wärend der Bilderzeugung interagieren zu können (Raw-Input-Data)
    public class RaytracingFrame3DData : Frame3DData
    {
        public RaytracingFrame3DData() { }
        public RaytracingFrame3DData(Frame3DData frameData)
            :base(frameData)
        {
        }

        public int ScreenWidth;
        public int ScreenHeight;
        public Action<string, float> ProgressChanged;
        public CancellationTokenSource StopTrigger; //Damit kann die Erstellung der Photonmap oder der Importance-Lights unterbrochen werden        
        public ImagePixelRange PixelRange;
    }
}

using System.Collections.Generic;
using GraphicMinimal;

namespace FullPathGenerator
{
    public class FullPathSampleResult
    {
        public bool MainPixelHitsBackground = false;        //Passiert, wenn EyeSubPath ins Leere fliegt
        public Vector3D RadianceFromRequestetPixel = null;    //RGB-Wert von den Pixel, für den die FullPaths erzeugt wurden
        public int CollectedVertexMergingPhotonCount = 0;

        //Hiermit kann man nach Pfadlänge/Pfadraum aufgesplittet die Einzelradiancen berechnen
        public List<FullPath> MainPaths = new List<FullPath>(); //All diese Pfade gehen durch den gleichen Pixel
        public List<FullPath> LighttracingPaths = new List<FullPath>();
    }
}

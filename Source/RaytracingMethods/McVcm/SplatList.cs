using BitmapHelper;
using FullPathGenerator;
using System.Collections;
using System.Collections.Generic;

namespace RaytracingMethods.McVcm
{
    //Liste von Pixel-Radiancewerten, welche in das Frame eingetragen werden sollen
    class SplatList
    {
        public List<FullPath> PT_DL = new List<FullPath>();
        public List<FullPath> VC_VM_LT = new List<FullPath>();

        public float Luminance { get; private set; } = 0; //Summe über die VC/VM/LT-Pfade

        //Wenn Fullpath per PT oder DL erzeugt wurde (LightSubPath wurde nicht per MarkovKette geändert)
        public void AddPathtracing(FullPath path)
        {
            this.PT_DL.Add(path);
        }

        //Wenn Fullpath per VC,VM,LT erzeugt wurde (LightSubPath wurde per MarkovKette geändert)
        public void AddLighttracing(FullPath path)
        {
            this.VC_VM_LT.Add(path);
            this.Luminance += PixelHelper.ColorToGray(path.Radiance);
        }
    }
}

using GraphicMinimal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaytracingMethods.McVcm
{
    class MarkovChain
    {
        public ChainState State;                        //u
        public SplatList SplatList = new SplatList();   //f(u)        
        public float CumulativeWeight = 0;              //Summe aller (1-Accept)-Werte, die man für jedes Proposed-Sample was nicht accepted wird erzeugt

        //Variablen um die Normalisierungskonstante zu aktualisieren        
        public float Normalization = 1;                 //Integral f(u)*du
        public int LargeStepCounter = 0;                //So viele LargeSteps hat diese Kette gemacht
        public float LargeStepTargetSum = 0;            //Summe über alle Target-Large-Step-Werte von der Kette

        public float AcceptWeightSum = 0;               //Summe aller Accept-Gewichte über alle SplatList-Samples. 

        //Bilddaten
        private ImagePixelRange pixelRange;
        public ImageBuffer Image;

        public MarkovChain(ChainSeed seed, ImagePixelRange pixelRange)
        {
            this.State = seed.State;
            this.SplatList = seed.Splat;
            this.pixelRange = pixelRange;
            this.Image = new ImageBuffer(pixelRange.Width, pixelRange.Height, new Vector3D(0, 0, 0));
        }

        public void Reset(ChainSeed seed)
        {
            this.State = seed.State;
            this.SplatList = seed.Splat;
            this.Image = new ImageBuffer(pixelRange.Width, pixelRange.Height, new Vector3D(0, 0, 0));
            this.AcceptWeightSum = 0;
        }

        public void AddPixel(Vector2D position, Vector3D color, float factor)
        {
            int rx = (int)Math.Floor(position.X - this.pixelRange.XStart);
            int ry = (int)Math.Floor(position.Y - this.pixelRange.YStart);
            if (rx >= 0 && rx < this.Image.Width && ry >= 0 && ry < this.Image.Height)
            {
                this.Image[rx, ry] += color * factor;
            }
        }

        public static void Swap(MarkovChain x, MarkovChain y)
        {
            //Swap State
            ChainState tempState = x.State;
            x.State = y.State;
            y.State = tempState;

            //Swap SplatList
            SplatList tempSplat = x.SplatList;
            x.SplatList = y.SplatList;
            y.SplatList = tempSplat;
        }

        public void UpdateNormalization()
        {
            if (this.LargeStepCounter != 0)
            {
                this.Normalization = this.LargeStepTargetSum / this.LargeStepCounter;
            }            
        }

        public float GetLuminanceCorrectionFactor()
        {
            float avgLuminance = this.AcceptWeightSum / this.pixelRange.Count;
            return this.Normalization / avgLuminance;
        }

        //Zu Analysezwecken um zu schauen, dass die AcceptRatio wirklich bie 23% liegt
        public float GetAcceptRatio()
        {
            return this.State.LightSampler.GetAcceptRatio();
        }
    }
}

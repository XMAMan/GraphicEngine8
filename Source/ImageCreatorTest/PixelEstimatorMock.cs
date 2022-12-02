using FullPathGenerator;
using GraphicGlobal;
using GraphicMinimal;
using RaytracingColorEstimator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCreatorTest
{
    class PixelEstimatorMock : IPixelEstimator, IFrameEstimator
    {
        public bool CreatesLigthPaths => false;

        private CancellationTokenSource stopTrigger;
        private int stopAfterXCalls;
        private int counter;

        private Exception errorToThrow;
        private string randomObjectBase64Coded;

        private int pixelSleepTime = 0;

        public IFrameEstimator CreateCopy()
        {
            return new PixelEstimatorMock()
            {
                stopTrigger = this.stopTrigger,
                stopAfterXCalls = this.stopAfterXCalls,
                counter = this.counter,
                errorToThrow = this.errorToThrow,
                randomObjectBase64Coded = this.randomObjectBase64Coded,
                pixelSleepTime = this.pixelSleepTime
            };
        }

        public PixelEstimatorMock()
        {
        }

        public PixelEstimatorMock(CancellationTokenSource stopTrigger, int stopAfterXCalls)
        {
            this.stopTrigger = stopTrigger;
            this.stopAfterXCalls = stopAfterXCalls;
            this.counter = 0;
        }

        public PixelEstimatorMock(Exception errorToThrow)
        {
            this.errorToThrow = errorToThrow;

            IRandom rand = new Rand(1); //1 = Thread Nummer 1 wirft die Exception
            for (int i = 0; i < 1000; i++) rand.NextDouble();
            this.randomObjectBase64Coded = rand.ToBase64String();
        }

        public PixelEstimatorMock(int pixelSleepTime)
        {
            this.pixelSleepTime = pixelSleepTime;
        }

        public void DoFramePrepareStep(int frameIterationNumber, IRandom rand)
        {
            Thread.Sleep(10);
        }



        public void BuildUp(RaytracingFrame3DData data)
        {
        }

        public FullPathSampleResult GetFullPathSampleResult(int x, int y, IRandom rand)
        {
            if (this.pixelSleepTime > 0) Thread.Sleep(this.pixelSleepTime);
            rand.NextDouble();
            this.counter++;
            if (this.stopTrigger != null && this.counter >= this.stopAfterXCalls)
            {
                this.stopTrigger.Cancel();
            }
            if (this.errorToThrow != null)
            {
                string randString = rand.ToBase64String();
                if (randString == this.randomObjectBase64Coded) throw this.errorToThrow;
            }
            return new FullPathSampleResult() { MainPixelHitsBackground = true }; //Nix getroffen
        }


    }
}

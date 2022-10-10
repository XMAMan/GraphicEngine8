using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParticipatingMedia;
using ParticipatingMedia.DistanceSampling;
using RaytracingRandom;

namespace PdfHistogram
{
    //Histogram für eine Distanzsamplefunktion, welche ein Distanzwert zwischen 0 und maxDistance sampelt
    public class DistanceHistogram
    {
        internal class Chunk
        {
            public List<RaySampleResult> Samples = new List<RaySampleResult>();
        }

        public class ChunkResult
        {
            public float PdfFromHistogram { get; private set; }
            public float PdfFromFunction { get; private set; }
            public float Error { get; private set; }
            public int SampleCount { get; private set; }

            internal ChunkResult(Chunk chunk, int sampleCount, float differantialLength)
            {
                if (chunk.Samples.Count > 0)
                {
                    this.PdfFromHistogram = chunk.Samples.Count / (float)sampleCount / differantialLength;
                    this.PdfFromFunction = chunk.Samples.Average(x => x.PdfL);
                    this.Error = Math.Abs(this.PdfFromHistogram - this.PdfFromFunction);
                    this.SampleCount = chunk.Samples.Count;
                }else
                {
                    this.PdfFromHistogram = -1;
                    this.PdfFromFunction = -1;
                    this.Error = 0;
                    this.SampleCount = 0;
                }
                
            }

            public override string ToString()
            {
                return PdfFromHistogram + " " + PdfFromFunction + " -> " + Error + "(" + SampleCount + ")";
            }
        }

        public class Result
        {
            public ChunkResult[] ChunkResults;
            public float MaxError;
            public string ErrorText;

            public Result(ChunkResult[] chunkResults)
            {
                this.ChunkResults = chunkResults;
                this.MaxError = chunkResults.Max(x => x.Error);
                this.ErrorText = string.Join(System.Environment.NewLine, chunkResults.OrderByDescending(x => x.Error).Select(x => x.ToString()));
            }
        }

        private float rayMin;
        private float rayMax;
        private float maxDistance;
        private Chunk[] chunks = null;
        private int sampleCount = 0;

        public DistanceHistogram(float rayMin, float rayMax, int chunkCount)
        {
            this.rayMin = rayMin;
            this.rayMax = rayMax;
            this.maxDistance = rayMax - rayMin;

            //Hinten das letzte Chunk hängt noch extra mit dran und enthält all die Samples, die dem maxDistance-Wert entsprechen
            //Die Pdf vom letzten Chunk enthält keine Wahrscheinlichkeitsdichte sondern die Wahrscheinlichkeit, dass das 
            //Medium ohne Scattering oder Absorbation durchlaufen wird. Also 1 - Pdf-Summe alle Chunks davor
            this.chunks = new Chunk[chunkCount + 1]; 
            for (int i = 0; i < this.chunks.Length; i++) this.chunks[i] = new Chunk();
        }

        public void AddSample(RaySampleResult sample)
        {
            this.sampleCount++;

            int index = -1;
            float t = sample.RayPosition - this.rayMin;
            if (t < this.maxDistance)
                index = Math.Min((int)(t / this.maxDistance * (this.chunks.Length - 1)), this.chunks.Length - 2);
            else
                index = this.chunks.Length - 1;

            this.chunks[index].Samples.Add(sample);
        }

        public Result GetResult()
        {
            float differantialLength = this.maxDistance / this.chunks.Length;
            float lengthFromLastChunk = 1; //Die Pdf vom letzten Chunk gibt an, wie viel Prozent aller Samples durchs Medium durchkamen. Somit teile ich nur durch 1, um eine Wahrscheinlichkeit und keine Wahrscheinlichkeitsdichte zu erhalten
            return new Result(this.chunks.Select(x => new ChunkResult(x, this.sampleCount, this.chunks.ToList().IndexOf(x) < this.chunks.Length - 1 ? differantialLength : lengthFromLastChunk)).ToArray());
        }

        public SimpleFunction GetPdfFunctionFromHistogram()
        {
            var chungs = GetResult().ChunkResults;
            return new SimpleFunction((x) =>
            {
                if (x < this.rayMin || x > this.rayMax) return 0;

                if (x == this.rayMax) return chungs.Last().PdfFromHistogram;

                double t = x - this.rayMin;
                int index = Math.Min((int)(t / this.maxDistance * (this.chunks.Length - 1)), this.chunks.Length - 2);

                return chungs[index].PdfFromHistogram;
            });
        }

        public SimpleFunction GetPdfFunctionFromPdfProperty()
        {
            var chungs = GetResult().ChunkResults;
            return new SimpleFunction((x) =>
            {
                if (x < this.rayMin || x > this.rayMax) return 0;

                if (x == this.rayMax) return chungs.Last().PdfFromFunction;

                double t = x - this.rayMin;
                int index = Math.Min((int)(t / this.maxDistance * (this.chunks.Length - 1)), this.chunks.Length - 2);

                return chungs[index].PdfFromFunction;
            });
        }
    }
}

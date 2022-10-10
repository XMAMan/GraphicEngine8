using System;
using System.Collections.Generic;
using GraphicMinimal;
using IntersectionTests;
using RayObjects.RayObjects;

namespace PdfHistogram
{
    //Es wird Menge von Quads als Input gegeben. Ich kann auf diesen Quads dann im generischen T-Parameter mithilfe von 3D-Punkt-Angabe was speichern
    class QuadListChunkTable<T> where T : new()
    {
        private Dictionary<IIntersectableRayDrawingObject, AxialQuadChunkTable<EntryWithIndizes>> data = new Dictionary<IIntersectableRayDrawingObject, AxialQuadChunkTable<EntryWithIndizes>>();

        public QuadListChunkTable(List<RayQuad> quads, int histogramSize)
        {
            for (int i = 0; i < quads.Count; i++)
            {
                Vector3D size = quads[i].MaxPoint - quads[i].MinPoint;
                int a1 = 1, a2 = 2; //Annahme: Axe 0 ist die kleinste Axe
                if (size.Y < size.X) { a1 = 0; } //Axe 1 doch kleiner als Axe 0?
                if (size.Z < size.Y) { a1 = 0; a2 = 1; } //Axe 2 nochmal kleiner als Axe 1?

                this.data[quads[i].RayHeigh] = new AxialQuadChunkTable<EntryWithIndizes>(quads[i].CenterOfGravity, a1, a2, Math.Max(size[a1], size[a2]), histogramSize);
                for (int x = 0; x < histogramSize; x++)
                    for (int y = 0; y < histogramSize; y++)
                    {
                        this.data[quads[i].RayHeigh].Data[x, y].Data.Index = i * histogramSize * histogramSize + x * histogramSize + y;
                        this.data[quads[i].RayHeigh].Data[x, y].Data.Data = new T();
                        this.data[quads[i].RayHeigh].Data[x, y].Data.DifferentialArea = this.data[quads[i].RayHeigh].DifferentialArea;
                    }
            }
        }

        public double DifferentialArea(IIntersectableRayDrawingObject rayHeight)
        {
            return this.data[rayHeight].DifferentialArea;
        }

        public EntryWithIndizes this[IntersectionPoint point]
        {
            get
            {
                return this.data[point.IntersectedRayHeigh][point.Position].Data;
            }
        }

        public IEnumerable<T> EntryCollection()
        {
            foreach (var key in this.data.Keys)
                for (int x = 0; x < this.data[key].Data.GetLength(0); x++)
                    for (int y = 0; y < this.data[key].Data.GetLength(1); y++)
                    {
                        yield return this.data[key].Data[x, y].Data.Data;
                    }
        }

        public IEnumerable<EntryWithIndizes> EntryCollectionWithIndizes()
        {
            foreach (var key in this.data.Keys)
                for (int x = 0; x < this.data[key].Data.GetLength(0); x++)
                    for (int y = 0; y < this.data[key].Data.GetLength(1); y++)
                    {
                        yield return this.data[key].Data[x, y].Data;
                    }
        }

        public IEnumerable<AxialQuadChunkTable<EntryWithIndizes>> GetQuadHistograms()
        {
            return this.data.Values;
        }

        public class EntryWithIndizes
        {
            public T Data;
            public int Index;
            public double DifferentialArea;
        }
    }
}

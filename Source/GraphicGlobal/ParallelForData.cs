using System;
using System.Collections.Generic;

namespace GraphicGlobal
{
    //Wenn ich bei einer Parallel.For-Anweisung Daten erzeuge, welche ich nach außen geben will, dann kann ich ich hiermit für jeden
    //Thread aus der Parallel-For-Schleife ein T-Objekt speichern
    //var threadIdCreator = new ParallelForData<object>(() => new object());    -> So könnte man mit threadIdCreator.GetThreadId() für jeden Renderthread ein Random-Seed-Wert ermitteln
    //var frames = new ParallelForData<ImageBuffer>(() => new ImageBuffer(100,100)); -> Die For-Schleife geht über alle Samples; Die Frame-Threads speichern alle in eigenen Bildpuffer ihre Daten
    public class ParallelForData<T> : List<T>
    {
        //https://stackoverflow.com/questions/32253912/get-a-thread-id-inside-parallel-foreach-loop
        private object locker = new object();
        private List<int> ids = new List<int>();

        private readonly Func<T> tFactory;

        public ParallelForData(Func<T> tFactory)
        {
            this.tFactory = tFactory;
        }

        public int GetThreadId()
        {
            int thread_id = ids.IndexOf(Environment.CurrentManagedThreadId);
            if (thread_id == -1)
            {
                ids.Add(Environment.CurrentManagedThreadId);
                thread_id = ids.IndexOf(Environment.CurrentManagedThreadId);
            }
            while (this.Count < thread_id + 1)
            {
                lock (this.locker)
                {
                    this.Add(this.tFactory());
                }
            }

            return thread_id;
        }

        public T GetDataFromCurrentThread()
        {
            return this[GetThreadId()];
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace PointSearch
{
    //----------------------------------------------------------------------
    //	ANNmin_k
    //		An ANNmin_k structure is one which maintains the smallest
    //		k values (of type PQKkey) and associated information (of type
    //		PQKinfo).  The special info and key values PQ_NULL_INFO and
    //		PQ_NULL_KEY means that thise entry is empty.
    //
    //		It is currently implemented using an array with k items.
    //		Items are stored in increasing sorted order, and insertions
    //		are made through standard insertion sort.  (This is quite
    //		inefficient, but current applications call for small values
    //		of k and relatively few insertions.)
    //		
    //		Note that the list contains k+1 entries, but the last entry
    //		is used as a simple placeholder and is otherwise ignored.
    //----------------------------------------------------------------------
    class SortedArray<T>
    {
        private int entryCount;     // number of keys currently active
        private KeyValuePair<float, T>[] entrys; // the list itself

        public SortedArray(int maxEntryCount)
        {
            this.entryCount = 0;
            this.entrys = new KeyValuePair<float, T>[maxEntryCount + 1];
        }

        public float GetMinimumKey()
        {
            return this.entryCount > 0 ? this.entrys[0].Key : float.MaxValue;
        }

        public float GetMaximumKey()
        {
            //return this.entryCount > 0 ? this.entrys[this.entryCount - 1].Key : float.MaxValue;
            return this.entryCount == this.entrys.Length - 1 ? this.entrys[this.entrys.Length - 2].Key : float.MaxValue;
        }

        public KeyValuePair<float, T> this[int index]
        {
            get
            {
                return index < this.entryCount ? this.entrys[index] : new KeyValuePair<float, T>(float.MaxValue, default(T));
            }
        }

        public void Insert(float key, T value)
        {
            int i = -1;
            for (i = this.entryCount; i > 0; i--)
            {
                if (this.entrys[i - 1].Key > key)
                    this.entrys[i] = this.entrys[i - 1];
                else
                    break;
            }
            this.entrys[i] = new KeyValuePair<float, T>(key, value);
            if (this.entryCount < this.entrys.Length - 1) this.entryCount++;
        }

        public List<T> GetAllValues()
        {
            return this.entrys.ToList().GetRange(0, this.entryCount).Select(x => x.Value).ToList();
        }
    }
}

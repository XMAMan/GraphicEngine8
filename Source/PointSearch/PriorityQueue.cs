using System;
using System.Collections.Generic;

namespace PointSearch
{
    //----------------------------------------------------------------------
    //	Priority queue
    //		A priority queue is a list of items, along with associated
    //		priorities.  The basic operations are insert and extract_minimum.
    //
    //		The priority queue is maintained using a standard binary heap.
    //		(Implementation note: Indexing is performed from [1..max] rather
    //		than the C standard of [0..max-1].  This simplifies parent/child
    //		computations.)  User information consists of a void pointer,
    //		and the user is responsible for casting this quantity into whatever
    //		useful form is desired.
    //
    //		Because the priority queue is so central to the efficiency of
    //		query processing, all the code is inline.
    //----------------------------------------------------------------------

    //Eine Liste, wo zu jeden Zeitpunkt (also nach jedem Insert oder ExtractMinimum) die entrys immer in aufsteigender Reihenfolge vorliegen
    class PriorityQueue<T>
    {
        private int entryCount;                     // number of items in queue
        private KeyValuePair<float, T>[] entrys;    // the priority queue (array of nodes) (Index 0 wird nicht benutzt; Erstes Element liegt bei 1)

        public PriorityQueue(int maxEntryCount)
        {
            this.entryCount = 0;
            this.entrys = new KeyValuePair<float, T>[maxEntryCount + 1];
        }

        public bool IsEmpty()
        {
            return this.entryCount == 0;
        }

        public bool IsNonEmpty()
        {
            return this.entryCount > 0;
        }

        public void Reset() // make existing queue empty
        {
            this.entryCount = 0;
        }

        //Fügt neues Element ans Ende von Array (Im Binärbaum ist das ganz unten Rechts) ein und Bubbeld es so lange hoch, bis Elternknoten > Einfügeposition ist
        //Der Binärbaum wird bei Index 1 beginned Zeilenweise numeriert (Index 0 bleibt frei)
        //ParentIndex berechnet sich aus aktuellerIndex / 2
        public void Insert(float key, T value)
        {
            if (++this.entryCount > this.entrys.Length - 1) throw new Exception("Priority queue overflow.");

            int insertPosition = this.entryCount;
            while (insertPosition > 1)                                  // sift up new item
            {
                int parent = insertPosition / 2;
                if (this.entrys[parent].Key <= key) break;              // in proper order

                this.entrys[insertPosition] = this.entrys[parent];      // else swap with parent
                insertPosition = parent;                                // Bubble Index up
            }
            this.entrys[insertPosition] = new KeyValuePair<float, T>(key, value);    // insert new item at final location
        }

        //Es wird das kleinste(Leichteste) Element, welches an Index 1 (Beim Baum ganz Oben der Wurzelknoten) liegt, 
        //weggenommen und dann nimmt man das größe(Schwerste) Element was ganz hinten im Array liegt, und läßt es 
        //bei Index 1 reinplumpsenm. Dort fällt es dann so lange runter, bis
        //es an einer Position angekommen ist, wo die Regel gilt 'Es muss leichter als seine Kinder sein'
        //LeftIndex  = ParentIndex * 2
        //RightIndex = ParentIndex * 2 + 1
        public KeyValuePair<float, T> ExtractMinimum()
        {
            KeyValuePair<float, T> minimum = this.entrys[1];            // Index 0 wird nicht benutzt

            float kn = this.entrys[this.entryCount--].Key;              // last item in queue
            int p = 1;                                                  // p points to item out of position
            int r = p << 1;                                             // left child of p
            while (r <= this.entryCount)                                // while r is still within the heap
            {
                if (r < this.entryCount && this.entrys[r].Key > this.entrys[r + 1].Key) r++;
                if (kn <= this.entrys[r].Key) break;                    // in proper order

                this.entrys[p] = this.entrys[r];                        // else swap with child
                p = r;                                                  // advance pointers
                r = p << 1;
            }
            this.entrys[p] = this.entrys[this.entryCount + 1];          // insert last item in proper place

            return minimum;
        }
    }
}

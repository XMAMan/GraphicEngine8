using System.Collections.Generic;
using GraphicMinimal;

namespace GraphicGlobal
{
    //Beschreibt eine Menge von 3D-Objekten, um daraus ein einzelnes Bild zu rendern
    public class Frame3DData
    {
        public GlobalObjectPropertys GlobalObjektPropertys;
        public List<DrawingObject> DrawingObjects;

        public Frame3DData() { }
        public Frame3DData(Frame3DData copy)
        {
            this.GlobalObjektPropertys = copy.GlobalObjektPropertys;
            this.DrawingObjects = copy.DrawingObjects;
        }
    }
}

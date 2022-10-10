using System.Collections.Generic;
using System.Drawing;

namespace GraphicMinimal
{
    //Ergebnis von GetFullPathsOverPixelRange
    public class PixelRangeResult
    {
        public string Text;
        public List<BitmapWithName> Images;
    }

    public class BitmapWithName
    {
        public Bitmap Image;
        public string Name;
    }
}

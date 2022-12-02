using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace GraphicMinimal
{
    //Wenn wärend des Aufrufs von IPixelEstimator.GetFullPathSampleResult oder IFrameEstimator.DoFramePrepareStep
    //eine Exception passiert, dann kann man den Fehler hiermit nachstellen. Ich speichere mir einfach die 
    //Input-Parameter für diese Funktionen. Grundannahme: Der Zustand vom IPixelEstimator-Objekt muss beim Fehler-
    //nachstellen der gleiche sein
    public class RaytracingDebuggingData
    {
        public class GetFullPathSampleResultParameter
        {
            public int PixX { get; set; }
            public int PixY { get; set; }
            public string RandomObjectBase64Coded { get; set; }
        }

        public class DoFramePrepareStepParameter
        {            
            public int FrameIterationNumber { get; set; }
            public string RandomObjectBase64Coded { get; set; }
        }

        public Size ScreenSize { get; set; }
        public ImagePixelRange PixelRange { get; set; }
        public GetFullPathSampleResultParameter PixelData { get; set; }
        public DoFramePrepareStepParameter FramePrepareData { get; set; }
        public GlobalObjectPropertys GlobalSettings { get; set; }

        public RaytracingDebuggingData() { } //Braucht der XML-Serialisierer


        //Wenn der Fehler bei ein Pixelverfahren auftritt
        public RaytracingDebuggingData(GetFullPathSampleResultParameter data, Size screenSize, ImagePixelRange pixelRange, GlobalObjectPropertys globalSettings)
        {
            this.PixelData = data;
            this.ScreenSize = screenSize;
            this.PixelRange = pixelRange;
            this.GlobalSettings = globalSettings;
        }

        //Wenn der Fehler beim Frame-PrepareStep auftritt
        public RaytracingDebuggingData(DoFramePrepareStepParameter data, Size screenSize, ImagePixelRange pixelRange, GlobalObjectPropertys globalSettings)
        {
            this.FramePrepareData = data;
            this.ScreenSize = screenSize;
            this.PixelRange = pixelRange;
            this.GlobalSettings = globalSettings;
        }

        //Wenn der Fehler beim Frame-Pixelstep auftritt
        public RaytracingDebuggingData(DoFramePrepareStepParameter frameData, GetFullPathSampleResultParameter pixelData, Size screenSize, ImagePixelRange pixelRange, GlobalObjectPropertys globalSettings)
        {
            this.FramePrepareData = frameData;
            this.PixelData = pixelData;
            this.ScreenSize = screenSize;
            this.PixelRange = pixelRange;
            this.GlobalSettings = globalSettings;
        }

        //https://stackoverflow.com/questions/2434534/serialize-an-object-to-string
        public string ToXmlString()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(this.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, this);
                string xmlString = textWriter.ToString();
                return "string xmlString = \"" + xmlString.Replace("\r", "").Replace("\n", "").Replace("\"", "\\\"") + "\";";
            }
        }

        public static RaytracingDebuggingData CreateFromXmlString(string xmlString)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(RaytracingDebuggingData));
            using (StringReader textReader = new StringReader(xmlString))
            {
                return (RaytracingDebuggingData)xmlSerializer.Deserialize(textReader);
            }
        }

        //string xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?><RaytracingDebuggingData xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">  <PixelData>    <PixX>309</PixX>    <PixY>239</PixY>    <RandomObjectBase64Coded>AAEAAAD/////AQAAAAAAAAAEAQAAAA1TeXN0ZW0uUmFuZG9tAwAAAAVpbmV4dAZpbmV4dHAJU2VlZEFycmF5AAAHCAgICQAAAB4AAAAJAgAAAA8CAAAAOAAAAAgAAAAA7ZBuAqXbZCMU+Ao3EUbTAf9m9FGHeIZbbx4fU0++9FPxs3YgvpUQYUXGRXoqPnNHq6pDQ2zvjQXm3w1Ym5tiMOZr220RSewUtZ87W+l89H+wVe4zggDJYGWcJRXwW9FiDQtDWsOScnDmSIxvKDwEfvLvJQDsQZVLXmLIWyvbhxmFJ8YMeSAaQ2r+qXmN8PtZpOViVwxTkVlMnYENCBu3aAd+4RR6ux51aAnSG+2UenL5MjkwrhW3c1CS7gXnSVRRO6BeYUBMhnadTrZ3kY1LVxVieAB/bCcMpKQuEws=</RandomObjectBase64Coded>  </PixelData></RaytracingDebuggingData>"; ---> 
        public static string ExtractDebuggingString(string exceptionText)
        {
            string start = "string xmlString = \"";
            string end = "\"; ---> ";
            int i1 = exceptionText.IndexOf(start) + start.Length;
            int i2 = exceptionText.IndexOf(end);
            return exceptionText.Substring(i1, i2 - i1);
        }
    }
}

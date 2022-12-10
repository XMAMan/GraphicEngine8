using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class ScaleImageDownArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw"), ArgRegex(@".*\.raw$")]
        public string Output { get; set; }


        [ArgRequired, ArgDefaultValue(5), ArgDescription("2 = 2*2Pixels creates a new pixel; 3 = 3*3 Pixels creates a new pixel"), ArgRange(1, 100)]
        public int ScaleFactor { get; set; }
    }
}

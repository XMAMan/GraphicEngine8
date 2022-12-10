using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class RemoveFireFlyArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw"), ArgRegex(@".*\.raw$")]
        public string Output { get; set; }


        [ArgDefaultValue(""), ArgDescription("Image which marks the areas where to search for fireflys. Supported Filestyps: bmp, jpg, png"), ArgRegex(@"(.*\.(bmp|jpg|png)$)|^$")]
        public string SearchMask { get; set; }
    }
}

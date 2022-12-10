using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class TonemappingTwoAreasArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw, bmp, jpg, png, hdr"), ArgRegex(@".*\.(raw|bmp|jpg|png|hdr)$")]
        public string Output { get; set; }


        [ArgRequired, ArgDefaultValue(1), ArgDescription("Brigthness-Value for the area marked with the mask")]
        public float Brigthness1 { get; set; }

        [ArgRequired, ArgDefaultValue(1), ArgDescription("Gamma-Value for the area marked with the mask")]
        public float Gamma1 { get; set; }

        [ArgRequired, ArgDefaultValue(1), ArgDescription("Brigthness-Value for the area marked with the negated mask")]
        public float Brigthness2 { get; set; }

        [ArgRequired, ArgDefaultValue(1), ArgDescription("Gamma-Value for the area marked with the negated mask")]
        public float Gamma2 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Image which marks the area 1. Supported Filestyps: bmp, jpg, png"), ArgRegex(@"(.*\.(bmp|jpg|png)$)|^$")]
        public string Mask { get; set; }
    }
}

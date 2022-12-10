using GraphicMinimal;
using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class TonemappingArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw, bmp, jpg, png, hdr"), ArgRegex(@".*\.(raw|bmp|jpg|png|hdr)$")]
        public string Output { get; set; }


        [ArgRequired, ArgDefaultValue(TonemappingMethod.None), ArgDescription("Values: None,GammaOnly,Reinhard,Ward,HaarmPeterDuikersCurve,JimHejlAndRichardBurgessDawson,Uncharted2Tonemap,ACESFilmicToneMappingCurve")]
        public TonemappingMethod Method { get; set; }
    }
}

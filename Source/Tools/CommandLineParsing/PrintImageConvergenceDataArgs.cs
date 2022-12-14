using PowerArgs;
using Tools.Tools.ImageConvergence;

namespace Tools.CommandLineParsing
{
    internal class PrintImageConvergenceDataArgs
    {
        [ArgRequired, ArgDescription("Filepath for the image which is used for comparing. Supported Filestyps: bmp, png, jpg"), ArgRegex(@".*\.(bmp|png|jpg)$")]
        public string ReferenceImageInputFile { get; set; }


        [ArgRequired, ArgDescription("Folder which was created via the CollectImageConvergenceData"), ArgExistingDirectory]
        public string DataFolder1 { get; set; }

        [ArgRequired, ArgDescription("Labeltext for DataFolder1")]
        public string Label1 { get; set; }



        [ArgDefaultValue(""), ArgDescription("Folder which was created via the CollectImageConvergenceData")]
        public string DataFolder2 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Labeltext for DataFolder2")]
        public string Label2 { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder which was created via the CollectImageConvergenceData")]
        public string DataFolder3 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Labeltext for DataFolder23")]
        public string Label3 { get; set; }



        [ArgDefaultValue(""), ArgDescription("Folder which was created via the CollectImageConvergenceData")]
        public string DataFolder4 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Labeltext for DataFolder4")]
        public string Label4 { get; set; }



        [ArgDefaultValue(""), ArgDescription("Folder which was created via the CollectImageConvergenceData")]
        public string DataFolder5 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Labeltext for DataFolder5")]
        public string Label5 { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder which was created via the CollectImageConvergenceData")]
        public string DataFolder6 { get; set; }

        [ArgDefaultValue(""), ArgDescription("Labeltext for DataFolder6")]
        public string Label6 { get; set; }


        [ArgRequired, ArgDescription("Width in pixel for the error-curve-image"), ArgRange(1, int.MaxValue)]
        public int Width { get; set; }

        [ArgRequired, ArgDescription("Height in pixel for the error-curve-image"), ArgRange(1, int.MaxValue)]
        public int Height { get; set; }

        [ArgDefaultValue(20), ArgDescription("Factor for the Compare-Images"), ArgRange(1, int.MaxValue)]
        public int ScaleUpFactor { get; set; }

        [ArgDefaultValue(110), ArgDescription("Factor for the Compare-Images"), ArgRange(1, 110)]
        public int MaxShownError { get; set; }


        [ArgDefaultValue(DataVisualizer.Layout.AllInRow), ArgDescription("Values: AllInRow, AllInColum")]
        public DataVisualizer.Layout Layout { get; set; }


        [ArgDefaultValue(DataVisualizer.MaxTime.Min), ArgDescription("Print curves up to to smalles/longest single-row. Values: Min, Max")]
        public DataVisualizer.MaxTime MaxTime { get; set; }        


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: jpg"), ArgRegex(@".*\.jpg$")]
        public string OutputImageFile { get; set; }
    }
}

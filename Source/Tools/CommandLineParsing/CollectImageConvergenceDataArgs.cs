using System;
using System.Linq;
using PowerArgs;
using GraphicPanels;
using System.Drawing;
using GraphicMinimal;
using System.Text.RegularExpressions;

namespace Tools.CommandLineParsing
{
    internal class CollectImageConvergenceDataArgs
    {
        [ArgRequired, ArgDescription("Json-File which describes the scene"), ArgExistingFile, ArgRegex(@".*json\.txt$"), ArgPosition(1)]
        public string SceneFile { get; set; }

        [ArgRequired, ArgDescription("Filepath for the image which is used for comparing. Supported Filestyps: bmp, png, jpg"), ArgRegex(@".*\.(bmp|png|jpg)$")]
        public string ReferenceImageInputFile { get; set; }


        [ArgRequired, ArgDescription("Folder where the ProgessImages and MAPE-File is saved")]
        public string OutputFolder { get; set; }


        [ArgDefaultValue(1), ArgDescription("How many samples will be calculated for each pixel")]
        public int SampleCount { get; set; }

        [ArgDefaultValue(10), ArgDescription("Timer-Tick-Rate in seconds for creating a MAPE-Samplepoint")]
        public int CollectionTimerTick { get; set; }

        [ArgDefaultValue(1), ArgDescription("After 'ProgressImageSaveRate' CollectionTimerTicks one ProgressImage will be saved")]
        public int ProgressImageSaveRate { get; set; }


        //So bekommt man die ganzen Werte hier: string.Join("\n", Enum.GetValues(typeof(Mode3D)).Cast<Mode3D>())

        [ArgRequired, ArgDescription("Values:OpenGL_Version_1_0,OpenGL_Version_3_0,Direct3D_11,CPU,RaytracerTest,Raytracer,PathTracer,BidirectionalPathTracing,FullBidirectionalPathTracing,Photonmapping,Photonmap,PhotonmapPixel,ProgressivePhotonmapping,VertexConnectionMerging,RadiositySolidAngle,RadiosityHemicube,MediaPathTracer,MediaBidirectionalPathTracing,MediaFullBidirectionalPathTracing,MediaEdgeSampler,UPBP,MediaBeamTracer,ThinMediaSingleScattering,ThinMediaSingleScatteringBiased,ThinMediaMultipleScattering")]
        public Mode3D RenderMod { get; set; }


        [ArgDefaultValue(TonemappingMethod.None), ArgDescription("Values: None,GammaOnly,Reinhard,Ward,HaarmPeterDuikersCurve,JimHejlAndRichardBurgessDawson,Uncharted2Tonemap,ACESFilmicToneMappingCurve")]
        public TonemappingMethod Tonemapping { get; set; }


        [ArgRequired, ArgDescription("Width in pixel for the output-image"), ArgRange(1, int.MaxValue)]
        public int Width { get; set; }


        [ArgRequired, ArgDescription("Height in pixel for the output-image"), ArgRange(1, int.MaxValue)]
        public int Height { get; set; }

        [ArgDefaultValue(""), ArgDescription("Subarea, which must be inside from 0..Width and 0..Height. Value: [MinX;MinY;MaxX;MaxY]")]
        public ImagePixelRange PixelRange { get; set; }

        [ArgReviver]
        public static ImagePixelRange Revive(string key, string val)
        {
            try
            {
                if (val == "") return null;

                var reg = new Regex(@"^\[(?<MinX>\d+);(?<MinY>\d+);(?<MaxX>\d+);(?<MaxY>\d+)\]$");
                if (reg.IsMatch(val) == false) throw new ArgException($"{val} does not match [Left;Up;Right;Down]");

                ImagePixelRange range = reg.Matches(val)
                    .Cast<Match>()
                    .Select(x => new ImagePixelRange(
                        new Point(Convert.ToInt32(x.Groups["MinX"].Value), Convert.ToInt32(x.Groups["MinY"].Value)),
                        new Point(Convert.ToInt32(x.Groups["MaxX"].Value), Convert.ToInt32(x.Groups["MaxY"].Value)))
                        )
                    .First();

                return range;
            }
            catch (Exception)
            {
                throw new ArgException("Not a valid ImagePixelRange: " + val);
            }
        }
    }
}

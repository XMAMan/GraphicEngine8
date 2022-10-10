using System;
using System.Linq;
using PowerArgs;
using GraphicPanels;
using System.Windows.Forms;
using Tools.Tools;
using Tools.Tools.SceneEditor;
using BitmapHelper;
using System.Drawing;
using GraphicMinimal;
using System.IO;
using System.Text.RegularExpressions;
using Tools.Tools.ImagePostProcessing;

namespace Tools
{
    internal class CommandLineExecutor
    {
        public static void ExecuteCommandLineAction(string[] args)
        {
            try
            {
                TryExecute(args);
            }
            catch (ArgException ex)
            {
                MessageBox.Show(ex.Message + "\n" + ArgUsage.GenerateUsageFromTemplate<CommandLineParser>());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private static void TryExecute(string[] args)
        {
            var parsed = Args.ParseAction<CommandLineParser>(args);

            switch (parsed.ActionArgsProperty.Name)
            {
                //Commandline-Arguments im VisualStudio: CreateImage ..\..\..\..\Data\01_RingSphere_json.txt -output ..\..\..\..\Scenes\01_RingSphere_CPU.jpg -renderMod CPU -width 420 -height 328
                //Commandline-Arguments im VisualStudio: CreateImage ..\..\..\..\Data\01_RingSphere_json.txt -output ..\..\..\..\Scenes\01_RingSphere_Raytracer.jpg -renderMod Raytracer -sampleCount 1 -width 420 -height 328 -closeWindowAfterRendering false
                //Commandline-Arguments im VisualStudio: CreateImage ..\..\..\..\Data\01_RingSphereWithDiffuseGroundAndNoMotionBlur_json.txt -output ..\..\..\..\Scenes\01_RingSphere_SubArea_CPU.jpg -renderMod CPU -width 420 -height 328 -pixelRange [34;144;101;250]
                //Commandline-Arguments im VisualStudio: CreateImage ..\..\..\..\Batch-Tools\ExeFolder\Data\27_MirrorsEdge_json.txt -output ..\..\..\..\Batch-Tools\ExeFolder\Scenes\27_MirrorsEdge.raw -renderMod ThinMediaMultipleScattering -tonemapping ACESFilmicToneMappingCurve -sampleCount 1 -width 192 -height 101 
                case "CreateImage":
                    {
                        var a = (parsed.ActionArgs as CreateImageArgs);
                        if (a.DataFolder == "") a.DataFolder = new FileInfo(a.SceneFile).DirectoryName + "\\";
                        if (GraphicPanel3D.IsRasterizerMode(a.RenderMod) && !a.Output.EndsWith(".bmp") && !a.Output.EndsWith(".jpg") && !a.Output.EndsWith(".png")) throw new ArgException("If the RenderMode is a RasterizerMode then only .bmp, .jpg and .png is allowd for Output");
                        var form = new SceneFileRenderer(parsed.ActionArgs as CreateImageArgs);
                        if (form.IsDisposed == false) Application.Run(form);                        
                    }
                    
                    break;

                //Commandline-Arguments im VisualStudio: SceneEditor ..\..\..\..\Data\ ..\SaveFolder
                case "SceneEditor":
                    {
                        var a = (parsed.ActionArgs as SceneEditorArgs);
                        Application.Run(new SceneEditor(a.DataFolder, a.SaveFolder));
                    }
                    break;

                //Commandline-Arguments im VisualStudio: ImageEditor
                case "ImageEditor":
                    {
                        Application.Run(new ImageEditor());
                    }
                    break;

                //Commandline-Arguments im VisualStudio: Test_2D -dataFolder ..\..\..\..\Data\
                case "Test_2D":
                    {
                        var a = (parsed.ActionArgs as DataFolderArgs);
                        Application.Run(new Form2DTest(a.DataFolder));
                    }
                    break;

                //Commandline-Arguments im VisualStudio: Test_3D -dataFolder ..\..\..\..\Data\
                case "Test_3D":
                    {
                        var a = (parsed.ActionArgs as DataFolderArgs);
                        Application.Run(new Form3DTest(a.DataFolder));
                    }
                    break;

                //Commandline-Arguments im VisualStudio: MasterTest Hoch -size 4 -dataFolder ..\..\..\..\Data\
                case "MasterTest":
                    {
                        var a = (parsed.ActionArgs as MasterTestArgs);
                        Application.Run(new Tools.MasterTestImage.MasterTestForm(a.Quality, a.Size, a.DataFolder));
                    }
                    break;

                //Commandline-Arguments im VisualStudio: CountLineOfCodes ..\..\..
                case "CountLineOfCodes":
                    {
                        var a = (parsed.ActionArgs as CountLineOfCodesArgs);
                        MessageBox.Show(LineOfCodesCounter.CountLineOfCodes(a.ProjectFolder));
                    }
                    break;

                //Commandline-Arguments im VisualStudio: CopyOnlyUsedData -scenesFolder ..\..\..\..\Scenes\ -dataSourceFolder ..\..\..\..\Data\ -dataDestinationFolder ..\..\..\..\SmallData\
                case "CopyOnlyUsedData":
                    {
                        var a = (parsed.ActionArgs as CopyOnlyUsedDataArgs);
                        CopyOnlyUsedFilesFromDataFolder.CopyOnlyUsedData(a.ScenesFolder, a.DataSourceFolder, a.DataDestinationFolder);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: RemoveFireFlys ..\..\..\Tools\China.raw -output ..\..\..\Tools\ChinaSmallNoFire.raw -searchMask ..\..\..\..\Data\ChinaFireMask.png
                //                                       RemoveFireFlys ..\..\..\..\Scenes\05_WaterCornellbox.raw -output ..\..\..\..\Scenes\05_WaterCornellboxNoFire.raw
                case "RemoveFireFlys":
                    {
                        var a = (parsed.ActionArgs as RemoveFireFlyArgs);
                        Bitmap searchMask = null;
                        if (a.SearchMask != "") searchMask = new Bitmap(a.SearchMask);
                        var rawImage = ImagePostProcessingHelper.ReadImageBufferFromFile(a.RawImageFile);
                        var outImage = rawImage.RemoveFireFlys(searchMask);
                        outImage.WriteToFile(a.Output);
                    }
                    break;

                case "ScaleImageDown":
                    {
                        var a = (parsed.ActionArgs as ScaleImageDownArgs);
                        var rawImage = ImagePostProcessingHelper.ReadImageBufferFromFile(a.RawImageFile);
                        var outImage = rawImage.ScaleSizeDown(a.ScaleFactor, false);
                        outImage.WriteToFile(a.Output);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: Tonemapping ..\..\..\Tools\ChinaSmallNoFire.raw -output ..\..\..\Tools\ChinaSmallNoFire.jpg -method ACESFilmicToneMappingCurve
                case "Tonemapping":
                    {
                        var a = (parsed.ActionArgs as TonemappingArgs);
                        var rawImage = ImagePostProcessingHelper.ReadImageBufferFromFile(a.RawImageFile);
                        ImagePostProcessingHelper.SaveImageBuffer(rawImage, a.Output, a.Method);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: TonemappingTwoAreas ..\..\..\Tools\SaeulenbueroSmall.raw -output ..\..\..\Tools\SaeulenbueroTwoParts.jpg -brigthness1 1 -gamma1 1,2 -brigthness2 0,31 -gamma2 1,637 -mask ..\..\..\..\Data\11_PillarsOfficeMask.jpg
                case "TonemappingTwoAreas":
                    {
                        var a = (parsed.ActionArgs as TonemappingTwoAreasArgs);
                        var rawImage = ImagePostProcessingHelper.ReadImageBufferFromFile(a.RawImageFile);
                        var image = rawImage.GammaAndBrighnessCorrectionTwoAreas(a.Brigthness1, a.Gamma1, a.Brigthness2, a.Gamma2, a.Mask);
                        ImagePostProcessingHelper.SaveImageBuffer(image, a.Output, TonemappingMethod.None);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: CreateCleanBatFile -projectFolder ..\..\..
                case "CreateCleanBatFile":
                    {
                        var a = (parsed.ActionArgs as CreateCleanBatFileArgs);
                        CleanBatCreator.CreateCleanFile(a.ProjectFolder);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: CreateILMergeBatFile -exeFolder . -ilMergeFilePath ..\..\..\..\Batch-Tools\ILMerge.exe -outputFileName CreateSingleExe.bat
                case "CreateILMergeBatFile":
                    {
                        var a = (parsed.ActionArgs as CreateILMergeBatFileArgs);
                        ILMergeBatCreator.CreateILMergeBatFile(a.ExeFolder, a.IlMergeFilePath, a.OutputFileName);
                    }
                    break;
                    
            }
        }
    }

    //Parst ein Commandline-String
    internal class CommandLineParser
    {
        [ArgActionMethod]
        public void CreateImage(CreateImageArgs args)
        {            
        }

        [ArgActionMethod]
        public void SceneEditor(SceneEditorArgs args)
        {
        }

        [ArgActionMethod]
        public void ImageEditor()
        {
        }

        [ArgActionMethod]
        public void Test_2D(DataFolderArgs args)
        {
        }

        [ArgActionMethod]
        public void Test_3D(DataFolderArgs args)
        {
        }

        [ArgActionMethod]
        public void MasterTest(MasterTestArgs args)
        {
        }

        [ArgActionMethod]
        public void CountLineOfCodes(CountLineOfCodesArgs args)
        {
        }

        [ArgActionMethod]
        public void CopyOnlyUsedData(CopyOnlyUsedDataArgs args)
        {
        }

        [ArgActionMethod]
        public void RemoveFireFlys(RemoveFireFlyArgs args)
        {
        }

        [ArgActionMethod]
        public void ScaleImageDown(ScaleImageDownArgs args)
        {
        }

        [ArgActionMethod]
        public void Tonemapping(TonemappingArgs args)
        {
        }

        [ArgActionMethod]
        public void TonemappingTwoAreas(TonemappingTwoAreasArgs args)
        {
        }

        [ArgActionMethod]
        public void CreateCleanBatFile(CreateCleanBatFileArgs args)
        {
        }

        [ArgActionMethod]
        public void CreateILMergeBatFile(CreateILMergeBatFileArgs args)
        {
        }
        
    }

    internal class CreateImageArgs
    {
        [ArgRequired, ArgDescription("Json-File which describes the scene"), ArgExistingFile, ArgRegex(@".*json\.txt$"), ArgPosition(1)]
        public string SceneFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw, bmp, png, jpg, hdr"), ArgRegex(@".*\.(raw|bmp|png|jpg|hdr)$")]
        public string Output { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder with all the obj- and texturefiles. If not defined, the folder from the Json-File will be taken")]
        public string DataFolder { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder where the data durring rendering are saved")]
        public string SaveFolder { get; set; }


        [ArgDefaultValue(-1), ArgDescription("How many samples will be calculated for each pixel")]
        public int SampleCount { get; set; }

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

        [ArgDefaultValue(true)]
        public bool CloseWindowAfterRendering { get; set; }


        [ArgDefaultValue(RadiosityColorMode.WithColorInterpolation), ArgDescription("Values: WithColorInterpolation,WithoutColorInterpolation")]
        public RadiosityColorMode RadiosityColorMode { get; set; }

        [ArgDefaultValue(0.01f), ArgDescription("The scene is divided into paches where each pach has a maxsize from RadiosityMaxAreaPerPatch")]
        public float RadiosityMaxAreaPerPatch { get; set; }
    }

    internal class SceneEditorArgs
    {
        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder where the data durring rendering are saved"), ArgPosition(2)]
        public string SaveFolder { get; set; }
    }

    internal class DataFolderArgs
    {
        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }
    }

    internal class MasterTestArgs
    {
        [ArgRequired, ArgDefaultValue(MasterTest.Accuracy.Normal), ArgDescription("Values:Ultra,Hoch,Mittel,Niedrig"), ArgPosition(1)]
        public MasterTest.Accuracy Quality { get; set; }


        [ArgRequired, ArgDefaultValue(4), ArgRange(1, 5), ArgDescription("1=[42,33] 2=[84,66] 3=[210,164] 4=[420,328] 5=[630,492]")]
        public int Size { get; set; }


        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }
    }

    internal class CountLineOfCodesArgs
    {
        [ArgRequired, ArgDescription("Folder for this project (Contains the sln-File)"), ArgExistingDirectory, ArgPosition(1)]
        public string ProjectFolder { get; set; }
    }

    internal class CopyOnlyUsedDataArgs
    {
        [ArgRequired, ArgDescription("Folder which contains the scenes.bat-files"), ArgExistingDirectory, ArgPosition(1)]
        public string ScenesFolder { get; set; }

        [ArgRequired, ArgDescription("Folder which contains the Json-/Obj- and Texture-Files"), ArgExistingDirectory, ArgPosition(2)]
        public string DataSourceFolder { get; set; }

        [ArgRequired, ArgDescription("Folder where only the used Files from Data-Folder are copied in"), ArgExistingDirectory, ArgPosition(2)]
        public string DataDestinationFolder { get; set; }
    }

    internal class RemoveFireFlyArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw"), ArgRegex(@".*\.raw$")]
        public string Output { get; set; }


        [ArgDefaultValue(""), ArgDescription("Image which marks the areas where to search for fireflys. Supported Filestyps: bmp, jpg, png"), ArgRegex(@"(.*\.(bmp|jpg|png)$)|^$")]
        public string SearchMask { get; set; }
    }

    internal class ScaleImageDownArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw"), ArgRegex(@".*\.raw$")]
        public string Output { get; set; }


        [ArgRequired, ArgDefaultValue(5), ArgDescription("2 = 2*2Pixels creates a new pixel; 3 = 3*3 Pixels creates a new pixel"), ArgRange(1, 100)]
        public int ScaleFactor { get; set; }
    }

    internal class TonemappingArgs
    {
        [ArgRequired, ArgDescription("Image-Data in raw-Format"), ArgExistingFile, ArgRegex(@".*\.raw$"), ArgPosition(1)]
        public string RawImageFile { get; set; }


        [ArgRequired, ArgDescription("Filepath for the image to be created. Supported Filestyps: raw, bmp, jpg, png, hdr"), ArgRegex(@".*\.(raw|bmp|jpg|png|hdr)$")]
        public string Output { get; set; }


        [ArgRequired, ArgDefaultValue(TonemappingMethod.None), ArgDescription("Values: None,GammaOnly,Reinhard,Ward,HaarmPeterDuikersCurve,JimHejlAndRichardBurgessDawson,Uncharted2Tonemap,ACESFilmicToneMappingCurve")]
        public TonemappingMethod Method { get; set; }
    }

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

    internal class CreateCleanBatFileArgs
    {
        [ArgRequired, ArgDescription("C#Folder with the sln-File"), ArgExistingDirectory, ArgPosition(1)]
        public string ProjectFolder { get; set; }
    }

    internal class CreateILMergeBatFileArgs
    {
        [ArgRequired, ArgDescription("Folder which contains the Exe-File from this project"), ArgExistingDirectory]
        public string ExeFolder { get; set; }

        [ArgRequired, ArgDescription("Path to the ILMerge.exe-File"), ArgExistingFile]
        public string IlMergeFilePath { get; set; }

        [ArgRequired, ArgDescription("Output-Batfilename"), ArgDefaultValue("CreateSingleExe.bat")]
        public string OutputFileName { get; set; }
    }
}

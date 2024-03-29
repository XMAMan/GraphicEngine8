﻿using System;
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
using Tools.Tools.ImagePostProcessing;
using Tools.Tools.ImageConvergence;
using System.Collections.Generic;

namespace Tools.CommandLineParsing
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

                //Commandline-Arguments im VisualStudio: SceneEditor ..\..\..\..\Data\ SaveFolder
                case "SceneEditor":
                    {
                        var a = (parsed.ActionArgs as SceneEditorArgs);
                        if (Directory.Exists(a.SaveFolder) == false) Directory.CreateDirectory(a.SaveFolder);
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
                        ImagePostProcessingHelper.SaveImageBuffer(rawImage, a.Output, a.Method, a.Brigthness);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: TonemappingTwoAreas ..\..\..\Tools\SaeulenbueroSmall.raw -output ..\..\..\Tools\SaeulenbueroTwoParts.jpg -brigthness1 1 -gamma1 1,2 -brigthness2 0,31 -gamma2 1,637 -mask ..\..\..\..\Data\11_PillarsOfficeMask.jpg
                case "TonemappingTwoAreas":
                    {
                        var a = (parsed.ActionArgs as TonemappingTwoAreasArgs);
                        var rawImage = ImagePostProcessingHelper.ReadImageBufferFromFile(a.RawImageFile);
                        var image = rawImage.GammaAndBrighnessCorrectionTwoAreas(a.Brigthness1, a.Gamma1, a.Brigthness2, a.Gamma2, a.Mask);
                        ImagePostProcessingHelper.SaveImageBuffer(image, a.Output, TonemappingMethod.None, 1);
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

                //Commandline-Arguments im VisualStudio: CollectImageConvergenceData ..\..\..\..\Data\05_MirrorCornellbox_json.txt -referenceImageInputFile ..\..\..\..\Scenes\05_MirrorCornellboxPixRange_Reference.bmp -outputFolder ..\..\..\..\Scenes\05_MirrorCornellBoxPixRangeData_BPT -sampleCount 1000 -collectionTimerTick 1 -renderMod MediaBidirectionalPathTracing -width 1073 -height 940 -pixelRange [730;819;746;835]
                case "CollectImageConvergenceData":
                    {
                        var form = new CollectImageConvergenceData(parsed.ActionArgs as CollectImageConvergenceDataArgs);
                        if (form.IsDisposed == false) Application.Run(form);
                    }
                    break;

                //Commandline-Arguments im VisualStudio: PrintImageConvergenceData -referenceImageInputFile ..\..\..\..\Scenes\05_MirrorCornellboxPixRange_Reference.bmp -dataFolder1 ..\..\..\..\Scenes\05_MirrorCornellBoxPixRangeData_BPT -label1 "Bidirectional Path Tracing" -dataFolder2 ..\..\..\..\Scenes\05_MirrorCornellBoxPixRangeData_MMLT -label2 "Multiplexed Metropolis Light Transport" -width 800 -height 600 -scaleUpFactor 20 -outputImageFile  ..\..\..\..\Scenes\05_MirrorCornellboxPixRange_BPT_MMLT_Compare.jpg
                case "PrintImageConvergenceData":
                    {
                        var a = (parsed.ActionArgs as PrintImageConvergenceDataArgs);

                        List<DataVisualizer.Folder> files = new List<DataVisualizer.Folder>();
                        files.Add(new DataVisualizer.Folder(a.DataFolder1, a.Label1));

                        if (string.IsNullOrEmpty(a.DataFolder2) == false)
                            files.Add(new DataVisualizer.Folder(a.DataFolder2, a.Label2));

                        if (string.IsNullOrEmpty(a.DataFolder3) == false)
                            files.Add(new DataVisualizer.Folder(a.DataFolder3, a.Label3));

                        if (string.IsNullOrEmpty(a.DataFolder4) == false)
                            files.Add(new DataVisualizer.Folder(a.DataFolder4, a.Label4));

                        if (string.IsNullOrEmpty(a.DataFolder5) == false)
                            files.Add(new DataVisualizer.Folder(a.DataFolder5, a.Label5));

                        if (string.IsNullOrEmpty(a.DataFolder6) == false)
                            files.Add(new DataVisualizer.Folder(a.DataFolder6, a.Label6));

                        new DataVisualizer(files.ToArray(), new Bitmap(a.ReferenceImageInputFile), a.MaxTime)
                            .GetCompareImage(a.Width, a.Height, a.ScaleUpFactor, a.Layout, a.MaxShownError)
                            .Save(a.OutputImageFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    break;

            }
        }
    }
}

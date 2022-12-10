using PowerArgs;

namespace Tools.CommandLineParsing
{
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
}

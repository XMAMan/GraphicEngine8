using PowerArgs;

namespace Tools.CommandLineParsing
{
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

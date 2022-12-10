using PowerArgs;


namespace Tools.CommandLineParsing
{
    internal class CreateCleanBatFileArgs
    {
        [ArgRequired, ArgDescription("C#Folder with the sln-File"), ArgExistingDirectory, ArgPosition(1)]
        public string ProjectFolder { get; set; }
    }
}

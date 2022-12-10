using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class CountLineOfCodesArgs
    {
        [ArgRequired, ArgDescription("Folder for this project (Contains the sln-File)"), ArgExistingDirectory, ArgPosition(1)]
        public string ProjectFolder { get; set; }
    }
}

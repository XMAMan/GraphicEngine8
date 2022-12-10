using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class DataFolderArgs
    {
        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }
    }
}

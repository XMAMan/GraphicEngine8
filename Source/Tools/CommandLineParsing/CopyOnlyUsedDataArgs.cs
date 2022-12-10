using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class CopyOnlyUsedDataArgs
    {
        [ArgRequired, ArgDescription("Folder which contains the scenes.bat-files"), ArgExistingDirectory, ArgPosition(1)]
        public string ScenesFolder { get; set; }

        [ArgRequired, ArgDescription("Folder which contains the Json-/Obj- and Texture-Files"), ArgExistingDirectory, ArgPosition(2)]
        public string DataSourceFolder { get; set; }

        [ArgRequired, ArgDescription("Folder where only the used Files from Data-Folder are copied in"), ArgExistingDirectory, ArgPosition(2)]
        public string DataDestinationFolder { get; set; }
    }
}

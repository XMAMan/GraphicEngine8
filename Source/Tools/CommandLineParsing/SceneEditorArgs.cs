using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class SceneEditorArgs
    {
        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }


        [ArgDefaultValue(""), ArgDescription("Folder where the data durring rendering are saved"), ArgPosition(2)]
        public string SaveFolder { get; set; }
    }
}

using PowerArgs;

namespace Tools.CommandLineParsing
{
    internal class MasterTestArgs
    {
        [ArgRequired, ArgDefaultValue(MasterTest.Accuracy.Normal), ArgDescription("Values:Ultra,Hoch,Mittel,Niedrig"), ArgPosition(1)]
        public MasterTest.Accuracy Quality { get; set; }


        [ArgRequired, ArgDefaultValue(4), ArgRange(1, 5), ArgDescription("1=[42,33] 2=[84,66] 3=[210,164] 4=[420,328] 5=[630,492]")]
        public int Size { get; set; }


        [ArgRequired, ArgDescription("Folder with all the obj- and texturefiles"), ArgExistingDirectory, ArgPosition(1)]
        public string DataFolder { get; set; }
    }
}

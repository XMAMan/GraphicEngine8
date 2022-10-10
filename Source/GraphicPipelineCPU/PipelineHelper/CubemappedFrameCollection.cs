using GraphicPipelineCPU.Textures;
using System.Collections.Generic;
using System.Linq;

namespace GraphicPipelineCPU.PipelineHelper
{
    class CubemappedFrameCollection
    {
        public Framebuffer OldBuffer = null;

        private Dictionary<int, CubemappedFrame> cubemaps = new Dictionary<int, CubemappedFrame>();

        public int CreateCubeMapFrame(int cubeMapSize = 256)
        {
            int newID = 1;
            if (this.cubemaps.Keys.Count > 0)
                newID = this.cubemaps.Keys.Max() + 1;

            this.cubemaps.Add(newID, new CubemappedFrame(cubeMapSize, cubeMapSize));

            return newID;
        }

        public CubemappedFrame this[int cubemapId]
        {
            get
            {
                return this.cubemaps[cubemapId];
            }
        }
    }
}

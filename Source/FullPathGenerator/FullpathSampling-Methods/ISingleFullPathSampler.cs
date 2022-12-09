using GraphicGlobal;
using SubpathGenerator;

namespace FullPathGenerator.FullpathSampling_Methods
{
    //Mit diesen Interface kann eine einzelne Fullpath-Sampling-Strategie benutzt werden (Wird fürs Multiplex Metropolis Sampling benötigt)
    public interface ISingleFullPathSampler : IFullPathSamplingMethod
    {
        FullPathSamplingStrategy[] GetAvailableStrategiesForFullPathLength(int fullPathLength);
        FullPath SampleFullPathFromSingleStrategy(SubPath eyePath, SubPath lightPath, int fullPathLength, int strategyIndex, IRandom rand);
    }

    public class FullPathSamplingStrategy
    {
        public int NeededEyePathLength;
        public int NeededLightPathLength;
        public int StrategyIndex;

        public FullPathSamplingStrategy() { }
        public FullPathSamplingStrategy(FullPathSamplingStrategy copy)
        {
            this.NeededEyePathLength = copy.NeededEyePathLength;
            this.NeededLightPathLength = copy.NeededLightPathLength;
            this.StrategyIndex = copy.StrategyIndex;
        }
    }
}

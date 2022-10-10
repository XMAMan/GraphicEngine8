using RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace.SlopeDistribution;
using System.Collections.Generic;

namespace RaytracingBrdf.BrdfFunctions.MicrofacetBrdf.Heiz.SlopSpace
{
    static class SlopeSpaceFactory
    {
        private static readonly object lockObject = new object();
        private static readonly Dictionary<string, ISlopeSpaceMicrofacet> instanceCache = new Dictionary<string, ISlopeSpaceMicrofacet>();
        public static ISlopeSpaceMicrofacet Build(float roughnessFactorX, float roughnessFactorY)
        {
            string key = (int)(roughnessFactorX * 100) + ";" + (int)(roughnessFactorY * 100);
            if (instanceCache.ContainsKey(key) == false)
            {
                lock (lockObject)
                {
                    if (instanceCache.ContainsKey(key) == false)
                        instanceCache.Add(key, new SlopeSpaceSmithMicrofacet(new GgxSlopeDistribution(), roughnessFactorX, roughnessFactorY));
                }
            }
            return instanceCache[key];
        }
    }
}

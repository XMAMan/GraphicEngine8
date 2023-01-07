namespace RaytracingMethods.McVcm
{
    //Ketten-Zustand u im PrimarySpace
    class ChainState
    {
        public readonly MLTSampler LightSampler; //Für das Light-Subpfad-Erstellen
        public readonly MLTSampler DirectSampler;//Für das DirectLighting        

        public ChainState(MLTSampler lightSampler, MLTSampler directSampler)
        {
            this.LightSampler = lightSampler;
            this.DirectSampler = directSampler;            
        }
    }
}

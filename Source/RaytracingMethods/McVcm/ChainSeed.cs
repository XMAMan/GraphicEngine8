namespace RaytracingMethods.McVcm
{
    //Hiermit wird die Kette initialisiert
    class ChainSeed
    {
        public readonly ChainState State;
        public readonly SplatList Splat;

        public ChainSeed(ChainState state, SplatList splat)
        {
            this.State = state;
            this.Splat = splat;
        }
    }
}

namespace PocketCity.Simulation
{
    public static class TerrainConstants
    {
        // Water thresholds
        public const float WaterThreshold = 0.72f;
        public const float ShallowWaterThreshold = 0.28f;
        public const float DeepWaterThreshold = 0.18f;

        // Noise parameters
        public const float WaterNoiseScale = 2.5f;
        public const float HillsNoiseScale = 0.64f;
        public const float HillsDetailScale = 0.12f;
        public const float HillsBlendFactor = 0.9f;

        // Hills thresholds
        public const int HillsElevationThreshold = 120;
        public const float HillsPrimaryThreshold = 0.78f;
        public const float HillsSecondaryThreshold = 0.68f;
    }
}

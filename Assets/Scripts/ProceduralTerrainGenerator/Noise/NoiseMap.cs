namespace ProceduralTerrainGenerator.Noise
{
    using UnityEngine;

    public static class NoiseMap
    {
        public static float[,] Generate( int width, int height )
        {
            float[,] noiseMap = new float[ width, height ];

            for( int x = 0; x < width; x++ )
            {
                for( int y = 0; y < height; y++ )
                {

                }
            }

            return noiseMap;
        }
    }
}
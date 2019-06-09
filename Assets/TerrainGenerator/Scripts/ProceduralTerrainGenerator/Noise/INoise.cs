namespace ProceduralTerrainGenerator.Noise
{
    using UnityEngine;

    public interface INoise
    {
        float Sample( float x );
        float Sample( float x, float y );
        float Sample( float x, float y, float z );
    }
}
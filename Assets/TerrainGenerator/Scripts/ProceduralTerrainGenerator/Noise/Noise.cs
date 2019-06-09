namespace ProceduralTerrainGenerator.Noise
{
    using UnityEngine;
    
    public abstract class Nosie : INoise
    {
        public abstract float Sample( float x );
        public abstract float Sample( float x, float y );
        public abstract float Sample( float x, float y, float z );
    }
}
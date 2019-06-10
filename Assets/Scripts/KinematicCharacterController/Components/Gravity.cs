namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Mathematics;

    [ System.Serializable ]
    public struct Gravity : IComponentData
    {
        public float3 Value;
    }
}
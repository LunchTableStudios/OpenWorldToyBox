namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct KinematicMovement : IComponentData
    {
        public float3 Value;
        public float3 Delta;
    }
}
namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Mathematics;

    public enum CharacterControllerState : byte
    {
        NONE = 0,
        GROUNDED
    }

    [ System.Serializable ]
    public struct CharacterController : IComponentData
    {
        public CharacterControllerState State;
        public float GroundTollerance;
        public float MaxSpeed;
        public float Acceleration;
        public float Friction;
        public float MaxSlope;
        public float3 TargetDirection;
    }
}
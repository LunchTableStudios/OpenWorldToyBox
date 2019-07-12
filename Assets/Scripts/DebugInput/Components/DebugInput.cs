namespace DebugInput
{
    using Unity.Entities;
    using KinematicCharacterController;

    [ System.Serializable ]
    public struct DebugInput : IComponentData
    {
        public float Speed;
    }
}
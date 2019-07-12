namespace KinematicCharacterController
{
    using Unity.Entities;

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
        public float MaxSlope;
    }
}
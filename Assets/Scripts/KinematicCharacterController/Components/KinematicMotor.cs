namespace KinematicCharacterController
{
    using Unity.Entities;
    
    [ System.Serializable ]
    public struct KinematicMotor : IComponentData
    {
        public int MaxCollisions;
        public float SkinWidth;
    }
}
namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Mathematics;

    [ System.Serializable ]
    public struct KinematicMotor : IComponentData
    {
        public int MaxIterations;
        public float SkinWidth;
    }
}
namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Collections;

    [ System.Serializable ]
    public struct KinematicMotor : IComponentData
    {
        public float SkinWidth;
        public NativeArray<ColliderCastHit> HorizontalColliderHits;
        public NativeArray<ColliderCastHit> VerticalColliderHits;
    }
}
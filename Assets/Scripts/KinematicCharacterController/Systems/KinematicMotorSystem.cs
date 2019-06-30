namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Physics;
    using Unity.Physics.Extensions;
    using Unity.Physics.Systems;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public class KinematicMotorSystem : JobComponentSystem
    {
        [ BurstCompile ]
        private unsafe struct MotorJob : IJobChunk
        {
            [ NativeDisableContainerSafetyRestriction ] public ArchetypeChunkComponentType<Translation> TranslationType;
            [ NativeDisableContainerSafetyRestriction ] public ArchetypeChunkComponentType<Rotation> RotationType;

            public ArchetypeChunkComponentType<PhysicsVelocity> PhysicsVelocityType;

            [ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

            [ ReadOnly ] public PhysicsWorld World;

            public void Execute( ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex )
            {
                NativeArray<Translation> chunkTranslations = chunk.GetNativeArray( TranslationType );
                NativeArray<Rotation> chunkRotations = chunk.GetNativeArray( RotationType );
                NativeArray<PhysicsCollider> chunkColliders = chunk.GetNativeArray( PhysicsColliderType );

                for( int i = 0; i < chunk.Count; i++ )
                {
                    Translation translation = chunkTranslations[i];
                    Rotation rotation = chunkRotations[i];
                    PhysicsCollider collider = chunkColliders[i];

                    CopyCollider( collider, out Collider* queryCollider );

                    RigidTransform rigidTransform = new RigidTransform
                    {
                        pos = translation.Value,
                        rot = rotation.Value
                    };
                }
            }

            private void CopyCollider( PhysicsCollider from, out Collider* to )
            {
                Collider* colliderPtr = from.ColliderPtr;

                byte* copiedColliderMemory = stackalloc byte[ colliderPtr->MemorySize ];
                to = ( Collider* )( copiedColliderMemory );
                UnsafeUtility.MemCpy( to, colliderPtr, colliderPtr->MemorySize );
                to->Filter = CollisionFilter.Default;
            }
        }

        private struct ApplyMovementToVelocityJob : IJobForEach<Movement, PhysicsVelocity>
        {
            public void Execute( [ ReadOnly ] ref Movement movement, ref PhysicsVelocity velocity )
            {
                velocity.Linear = movement.Delta;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies )
        {
            ApplyMovementToVelocityJob applyMovementJob = new ApplyMovementToVelocityJob();
            inputDependencies = applyMovementJob.Schedule( this, inputDependencies );

            return inputDependencies;
        }
    }
}
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
        private const int MAX_COLLIDER_QUERIES = 64;

        private BuildPhysicsWorld m_buildPhysicsWorld;

        private EntityQuery m_motorQuery;

        [ BurstCompile ]
        private unsafe struct MotorJob : IJobChunk
        {
            [ ReadOnly ] public PhysicsWorld World;
            [ ReadOnly ] public ArchetypeChunkComponentType<KinematicMotor> KinematicMotorType;
            [ ReadOnly ] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

            public float DeltaTime;

            public ArchetypeChunkComponentType<Translation> TranslationType;
            public ArchetypeChunkComponentType<Rotation> RotationType;
            public ArchetypeChunkComponentType<Movement> MovementType;

            public NativeArray<DistanceHit> DistanceHits;
            public NativeArray<ColliderCastHit> ColliderCastHits;
            public NativeArray<SurfaceConstraintInfo> ConstraintInfos;

            public void Execute( ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex )
            {
                NativeArray<KinematicMotor> chunkMotors = chunk.GetNativeArray( KinematicMotorType );
                NativeArray<Movement> chunkMovements = chunk.GetNativeArray( MovementType );
                NativeArray<PhysicsCollider> chunkColliders = chunk.GetNativeArray( PhysicsColliderType );
                NativeArray<Translation> chunkTranslations = chunk.GetNativeArray( TranslationType );
                NativeArray<Rotation> chunkRotations = chunk.GetNativeArray( RotationType );

                for( int i = 0; i < chunk.Count; i++ )
                {
                    KinematicMotor motor = chunkMotors[i];
                    Movement movement = chunkMovements[i];
                    PhysicsCollider collider = chunkColliders[i];
                    Translation translation = chunkTranslations[i];
                    Rotation rotation = chunkRotations[i];

                    CopyCollider( collider, out Collider* queryCollider );

                    RigidTransform rigidTransform = new RigidTransform
                    {
                        pos = translation.Value,
                        rot = rotation.Value
                    };

                    KinematicMotorUtilities.HandleMotorConstraints( World, rigidTransform, queryCollider, motor.SkinWidth, DeltaTime, ref DistanceHits, ref ConstraintInfos );
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

        protected override void OnCreate()
        {
            m_buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            m_motorQuery = GetEntityQuery(
                typeof( KinematicMotor ),
                typeof( Movement ),
                typeof( PhysicsCollider ),
                typeof( PhysicsVelocity ),
                typeof( Translation ),
                typeof( Rotation )
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies )
        {
            NativeArray<ArchetypeChunk> chunks = m_motorQuery.CreateArchetypeChunkArray( Allocator.TempJob );

            ArchetypeChunkComponentType<KinematicMotor> chunkKinematicMotorType = GetArchetypeChunkComponentType<KinematicMotor>();
            ArchetypeChunkComponentType<Movement> chunkMovementType = GetArchetypeChunkComponentType<Movement>();
            ArchetypeChunkComponentType<PhysicsCollider> chunkPhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>();
            ArchetypeChunkComponentType<PhysicsVelocity> chunkPhysicsVelocityType = GetArchetypeChunkComponentType<PhysicsVelocity>();
            ArchetypeChunkComponentType<Translation> chunkTranslationType = GetArchetypeChunkComponentType<Translation>();
            ArchetypeChunkComponentType<Rotation> chunkRotationType = GetArchetypeChunkComponentType<Rotation>();

            MotorJob motorJob = new MotorJob
            {
                World = m_buildPhysicsWorld.PhysicsWorld,
                DeltaTime = UnityEngine.Time.deltaTime,
                KinematicMotorType = chunkKinematicMotorType,
                PhysicsColliderType = chunkPhysicsColliderType,
                MovementType = chunkMovementType,
                TranslationType = chunkTranslationType,
                RotationType = chunkRotationType,

                DistanceHits = new NativeArray<DistanceHit>( MAX_COLLIDER_QUERIES, Allocator.TempJob ),
                ColliderCastHits = new NativeArray<ColliderCastHit>( MAX_COLLIDER_QUERIES, Allocator.TempJob ),
                ConstraintInfos = new NativeArray<SurfaceConstraintInfo>( MAX_COLLIDER_QUERIES * 4, Allocator.TempJob )
            };
            inputDependencies = motorJob.Schedule( m_motorQuery, inputDependencies );

            ApplyMovementToVelocityJob applyMovementJob = new ApplyMovementToVelocityJob();
            inputDependencies = applyMovementJob.Schedule( this, inputDependencies );

            return inputDependencies;
        }
    }
}
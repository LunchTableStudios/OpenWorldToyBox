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

    [ UpdateAfter( typeof( MovementSystem ) ) ]
    public class KinematicMotorSystem : JobComponentSystem
    {
        private const int MAX_COLLIDER_QUERIES = 64;

        private ExportPhysicsWorld m_ExportPhysicsWorldSystem;
        private BuildPhysicsWorld m_buildPhysicsWorld;

        private EntityQuery m_motorQuery;

        [ BurstCompile ]
        private unsafe struct MotorJob : IJobChunk
        {
            public float DeltaTime;

            [ ReadOnly ] public PhysicsWorld World;
            [ ReadOnly ] public ArchetypeChunkComponentType<KinematicMotor> KinematicMotorType;
            [ ReadOnly ] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

            [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Translation> TranslationType;
            [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Rotation> RotationType;
            [NativeDisableContainerSafetyRestriction] public ArchetypeChunkComponentType<Movement> MovementType;

            [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
            [DeallocateOnJobCompletion] public NativeArray<DistanceHit> DistanceHits;
            [DeallocateOnJobCompletion] public NativeArray<ColliderCastHit> ColliderCastHits;
            [DeallocateOnJobCompletion] public NativeArray<SurfaceConstraintInfo> ConstraintInfos;

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

                    RigidTransform rigidTransform = new RigidTransform
                    {
                        pos = translation.Value,
                        rot = rotation.Value
                    };

                    translation.Value = rigidTransform.pos;
                    rotation.Value = rigidTransform.rot;

                    // Fill out debug properties for visualization
                    {

                    }

                    // Write data back to chunk
                    {
                        chunkTranslations[i] = translation;
                        chunkRotations[i] = rotation;
                    }
                }
            }

            private void CopyCollider( PhysicsCollider from, out Collider* to )
            {
                Collider* colliderPtr = from.ColliderPtr;

                byte* copiedColliderMemory = stackalloc byte[ colliderPtr -> MemorySize ];
                to = ( Collider* )( copiedColliderMemory );
                UnsafeUtility.MemCpy( to, colliderPtr, colliderPtr -> MemorySize );
                to -> Filter = CollisionFilter.Default;
            }
        }

        private struct ApplyMovementToVelocityJob : IJobForEach<Movement, PhysicsVelocity>
        {
            public void Execute( [ ReadOnly ] ref Movement movement, ref PhysicsVelocity velocity )
            {
                velocity.Linear = movement.Value;
            }
        }

        protected override void OnCreate()
        {
            m_ExportPhysicsWorldSystem = World.GetOrCreateSystem<ExportPhysicsWorld>();
            m_buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            m_motorQuery = GetEntityQuery(
                typeof( KinematicMotor ),
                typeof( Movement ),
                typeof( PhysicsCollider ),
                typeof( Translation ),
                typeof( Rotation )
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies )
        {
            m_ExportPhysicsWorldSystem.FinalJobHandle.Complete(); // Without this the update seems a bit more jittery

            NativeArray<ArchetypeChunk> chunks = m_motorQuery.CreateArchetypeChunkArray( Allocator.TempJob );

            ArchetypeChunkComponentType<KinematicMotor> chunkKinematicMotorType = GetArchetypeChunkComponentType<KinematicMotor>();
            ArchetypeChunkComponentType<Movement> chunkMovementType = GetArchetypeChunkComponentType<Movement>();
            ArchetypeChunkComponentType<PhysicsCollider> chunkPhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>();
            ArchetypeChunkComponentType<Translation> chunkTranslationType = GetArchetypeChunkComponentType<Translation>();
            ArchetypeChunkComponentType<Rotation> chunkRotationType = GetArchetypeChunkComponentType<Rotation>();

            MotorJob motorJob = new MotorJob
            {
                Chunks = chunks,

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

            return inputDependencies;
        }
    }
}
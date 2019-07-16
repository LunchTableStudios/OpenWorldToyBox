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
        private ExportPhysicsWorld m_ExportPhysicsWorldSystem;
        private BuildPhysicsWorld m_buildPhysicsWorld;

        private EntityQuery m_motorQuery;

        [ BurstCompile ]
        private struct MotorJob : IJobChunk
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
            [DeallocateOnJobCompletion] public NativeArray<SurfaceConstraintInfo> SurfaceConstraintInfos;

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

                    RigidTransform transform = new RigidTransform
                    {
                        pos = translation.Value,
                        rot = rotation.Value
                    };

                    float3 velocity = movement.Value;

                    unsafe
                    {
                        Collider* queryCollider;
                        {
                            Collider* colliderPtr = collider.ColliderPtr;

                            byte* copiedColliderMemory = stackalloc byte[colliderPtr->MemorySize];
                            queryCollider = (Collider*)(copiedColliderMemory);
                            UnsafeUtility.MemCpy(queryCollider, colliderPtr, colliderPtr->MemorySize);
                            queryCollider->Filter = CollisionFilter.Default;
                        }

                        KinematicMotorUtilities.SolveCollisionConstraints( World, DeltaTime, motor.MaxIterations, motor.SkinWidth, 360, queryCollider, ref transform, ref velocity, ref DistanceHits, ref ColliderCastHits, ref SurfaceConstraintInfos );
                    }

                    translation.Value = transform.pos;
                    movement.Value = velocity;

                    // Apply data back to chunk
                    {
                        chunkTranslations[i] = translation;
                        chunkMovements[i] = movement;
                    }
                }
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
            m_ExportPhysicsWorldSystem.FinalJobHandle.Complete();

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

                DeltaTime = UnityEngine.Time.fixedDeltaTime,

                KinematicMotorType = chunkKinematicMotorType,
                PhysicsColliderType = chunkPhysicsColliderType,
                MovementType = chunkMovementType,
                TranslationType = chunkTranslationType,
                RotationType = chunkRotationType,

                DistanceHits = new NativeArray<DistanceHit>( KinematicMotorUtilities.MAX_QUERIES, Allocator.TempJob ),
                ColliderCastHits = new NativeArray<ColliderCastHit>( KinematicMotorUtilities.MAX_QUERIES, Allocator.TempJob ),
                SurfaceConstraintInfos = new NativeArray<SurfaceConstraintInfo>( KinematicMotorUtilities.MAX_QUERIES * 4, Allocator.TempJob )
            };
            inputDependencies = motorJob.Schedule( m_motorQuery, inputDependencies );

            return inputDependencies;
        }
    }
}
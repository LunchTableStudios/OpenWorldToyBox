namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Physics;
    using Unity.Physics.Systems;
    using Unity.Transforms;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public class CharacterControllerSystem : JobComponentSystem
    {   
        private ExportPhysicsWorld m_exportPhysicsWorldSystem;
        private BuildPhysicsWorld m_buildPhysicsWorld;

        private EntityQuery m_characterControllerQuery;

        [ BurstCompile ]
        private struct CheckStateJob : IJobChunk
        {
            [ NativeDisableContainerSafetyRestriction ] public ArchetypeChunkComponentType<CharacterController> CharacterControllerType;

            [ ReadOnly ] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;
            [ ReadOnly ] public ArchetypeChunkComponentType<Translation> TranslationType;
            [ ReadOnly ] public ArchetypeChunkComponentType<Rotation> RotationType;

            [ ReadOnly ] public PhysicsWorld World;
            [ ReadOnly ] public float DeltaTime;

            [ DeallocateOnJobCompletion ] public NativeArray<ArchetypeChunk> Chunks;
            [ DeallocateOnJobCompletion ] public NativeArray<DistanceHit> DistanceHits;
            [ DeallocateOnJobCompletion ] public NativeArray<SurfaceConstraintInfo> SurfaceConstraintInfos;

            public void Execute( ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex )
            {
                NativeArray<CharacterController> chunkCharacterControllers = chunk.GetNativeArray( CharacterControllerType );
                NativeArray<PhysicsCollider> chunkPhysicsColliders = chunk.GetNativeArray( PhysicsColliderType );
                NativeArray<Translation> chunkTranslations = chunk.GetNativeArray( TranslationType );
                NativeArray<Rotation> chunkRotations = chunk.GetNativeArray( RotationType );

                for( int i = 0; i < chunk.Count; i++ )
                {
                    CharacterController controller = chunkCharacterControllers[i];
                    PhysicsCollider collider = chunkPhysicsColliders[i];
                    Translation translation = chunkTranslations[i];
                    Rotation rotation = chunkRotations[i];

                    RigidTransform transform = new RigidTransform
                    {
                        pos = translation.Value,
                        rot = rotation.Value
                    };

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
                        
                        KinematicMotorUtilities.MaxHitCollector<DistanceHit> distanceHitCollector = new KinematicMotorUtilities.MaxHitCollector<DistanceHit>( controller.GroundTollerance, ref DistanceHits );
                        {
                            ColliderDistanceInput input = new ColliderDistanceInput
                            {
                                MaxDistance = controller.GroundTollerance,
                                Transform = transform,
                                Collider = queryCollider
                            };
                            World.CalculateDistance( input, ref distanceHitCollector );
                        }

                        for( int hitIndex = 0; hitIndex < distanceHitCollector.NumHits; hitIndex++ )
                        {
                            DistanceHit hit = distanceHitCollector.AllHits[ hitIndex ];
                            KinematicMotorUtilities.CreateConstraintFromHit( World, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, float3.zero, hit.SurfaceNormal, hit.Distance, DeltaTime, out SurfaceConstraintInfo constraint );
                            SurfaceConstraintInfos[ hitIndex ] = constraint;
                        }

                        float3 outPosition = transform.pos;
                        float3 outVelocity = -math.up();
                        SimplexSolver.Solve( World, DeltaTime, math.up(), distanceHitCollector.NumHits, ref SurfaceConstraintInfos, ref outPosition, ref outVelocity, out float integratedTime );

                        if( distanceHitCollector.NumHits == 0 )
                        {
                            controller.State = CharacterControllerState.NONE;
                        }
                        else
                        {
                            outVelocity = math.normalize( outVelocity );
                            float slopeAngleSin = math.dot( outVelocity, -math.up() );
                            float slopeAngleCosSq = 1 - slopeAngleSin * slopeAngleSin;
                            float maxSlopeCos = math.cos( controller.MaxSlope );

                            controller.State = CharacterControllerState.GROUNDED;
                        }
                    }

                    // Apply data back to chunk
                    {
                        chunkCharacterControllers[i] = controller;
                    }
                }
            }
        }

        protected override void OnCreate()
        {
            m_exportPhysicsWorldSystem = World.GetOrCreateSystem<ExportPhysicsWorld>();
            m_buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

            m_characterControllerQuery = GetEntityQuery(
                typeof( CharacterController ),
                typeof( PhysicsCollider ),
                typeof( Translation ),
                typeof( Rotation )
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies)
        {
            m_exportPhysicsWorldSystem.FinalJobHandle.Complete();
            
            NativeArray<ArchetypeChunk> chunks = m_characterControllerQuery.CreateArchetypeChunkArray( Allocator.TempJob );

            ArchetypeChunkComponentType<CharacterController> chunkCharacterControllerType = GetArchetypeChunkComponentType<CharacterController>();
            ArchetypeChunkComponentType<PhysicsCollider> chunkPhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>();
            ArchetypeChunkComponentType<Translation> chunkTranslationType = GetArchetypeChunkComponentType<Translation>();
            ArchetypeChunkComponentType<Rotation> chunkRotationType = GetArchetypeChunkComponentType<Rotation>();

            CheckStateJob checkStateJob = new CheckStateJob
            {
                Chunks = chunks,

                World = m_buildPhysicsWorld.PhysicsWorld,

                DeltaTime = UnityEngine.Time.deltaTime,

                CharacterControllerType = chunkCharacterControllerType,
                PhysicsColliderType = chunkPhysicsColliderType,
                TranslationType = chunkTranslationType,
                RotationType = chunkRotationType,

                DistanceHits = new NativeArray<DistanceHit>( KinematicMotorUtilities.MAX_QUERIES, Allocator.TempJob ),
                SurfaceConstraintInfos = new NativeArray<SurfaceConstraintInfo>( KinematicMotorUtilities.MAX_QUERIES, Allocator.TempJob )
            };
            inputDependencies = checkStateJob.Schedule( m_characterControllerQuery, inputDependencies );

            return inputDependencies;
        } 
    }
}
namespace KinematicCharacterController
{
    using Unity.Physics;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static class KinematicMotorUtilities
    {
        public const int MAX_QUERIES = 64;

        public struct MaxHitCollector<T> : ICollector<T> where T : struct, IQueryResult
        {
            private int m_numHits;
            
            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction{ get; }
            public int NumHits => m_numHits;

            public NativeArray<T> AllHits;

            public MaxHitCollector( float maxFraction, ref NativeArray<T> allHits )
            {
                MaxFraction = maxFraction;
                AllHits = allHits;
                m_numHits = 0;
            }

            public bool AddHit( T hit )
            {
                AllHits[ m_numHits ] = hit;
                m_numHits++;
                return true;
            }

            public void TransformNewHits( int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey )
            {
                for( int i = oldNumHits; i < m_numHits; i++ )
                {
                    T hit = AllHits[i];
                    hit.Transform( transform, numSubKeyBits, subKey );
                    AllHits[i] = hit;
                }
            }

            public void TransformNewHits( int oldNumHits, float oldFraction, Math.MTransform transform, int rigidbodyIndex )
            {
                for( int i = oldNumHits; i < m_numHits; i++ )
                {
                    T hit = AllHits[i];
                    hit.Transform( transform, rigidbodyIndex );
                    AllHits[i] = hit;
                }
            }
        }

        public static void CreateConstraintFromHit( PhysicsWorld world, ColliderKey key, int rigidbodyIndex, float3 position, float3 normal, float distance, float deltaTime, out SurfaceConstraintInfo constraint )
        {
            bool dynamicBody = 0 <= rigidbodyIndex && rigidbodyIndex < world.NumDynamicBodies;

            constraint = new SurfaceConstraintInfo
            {
                Plane = new Plane
                {
                    Normal = normal,
                    Distance = distance
                },
                RigidBodyIndex = rigidbodyIndex,
                ColliderKey = key,
                HitPosition = position,
                Velocity = dynamicBody ? world.MotionVelocities[ rigidbodyIndex ].LinearVelocity : float3.zero
            };

            if( distance < 0.0f )
            {
                constraint.Velocity = constraint.Velocity - constraint.Plane.Normal * distance;
            }
        }

        public static unsafe void SolveCollisionConstraints( PhysicsWorld world, float deltaTime, int maxIterations, float skinWidth, Collider* collider, ref RigidTransform transform, ref float3 velocity, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> colliderHits, ref NativeArray<SurfaceConstraintInfo> surfaceConstraints )
        {
            float remainingTime = deltaTime;
            // float3 previousDisplacement = velocity * remainingTime;

            float3 outPosition = transform.pos;
            float3 outVelocity = velocity;

            quaternion orientation = transform.rot;

            const float timeEpsilon = 0.000001f;

            for( int i = 0; i < maxIterations && remainingTime > timeEpsilon; i++ )
            {
                MaxHitCollector<DistanceHit> distanceHitCollector = new MaxHitCollector<DistanceHit>( skinWidth, ref distanceHits );
                MaxHitCollector<ColliderCastHit> colliderHitCollector = new MaxHitCollector<ColliderCastHit>( 1.0f, ref colliderHits );

                int constraintCount = 0;

                // Handle distance checks
                {
                    ColliderDistanceInput input = new ColliderDistanceInput
                    {
                        Collider = collider,
                        MaxDistance = skinWidth,
                        Transform = new RigidTransform
                        {
                            pos = outPosition,
                            rot = orientation
                        }
                    };
                    world.CalculateDistance( input, ref distanceHitCollector );

                    for( int hitIndex = 0; hitIndex < distanceHitCollector.NumHits; hitIndex++ )
                    {
                        DistanceHit hit = distanceHitCollector.AllHits[ hitIndex ];
                        CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, hit.SurfaceNormal, hit.Distance, deltaTime, out SurfaceConstraintInfo constraint );
                        surfaceConstraints[ constraintCount++ ] = constraint;
                    }
                }

                float3 previousPosition = outPosition;
                float3 previousVelocity = outVelocity;

                SimplexSolver.Solve( world, remainingTime, math.up(), constraintCount, ref surfaceConstraints, ref outPosition, ref outVelocity, out float integratedTime );

                float3 currentDisplacement = outPosition - previousPosition;

                if( math.lengthsq( currentDisplacement ) > SimplexSolver.c_SimplexSolverEpsilon )
                {
                    outPosition = previousPosition + currentDisplacement;
                }

                remainingTime -= integratedTime;

                // previousDisplacement = outVelocity * remainingTime;
            }

            transform.pos = outPosition;
            velocity = outVelocity;
        }
    }
}
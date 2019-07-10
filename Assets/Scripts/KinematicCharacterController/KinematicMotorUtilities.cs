namespace KinematicCharacterController
{
    using Unity.Physics;
    using Unity.Mathematics;
    using Unity.Collections;

    public static class KinematicMotorUtilities
    {
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

        public static unsafe void SolveMotorConstraints( PhysicsWorld world, Collider* collider, float deltaTime, int maxIterations, ref RigidTransform transform, ref float3 linearVelocity, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> colliderHits, ref NativeArray<SurfaceConstraintInfo> constraintInfos )
        {
            transform.pos = transform.pos + linearVelocity * deltaTime;

            float3 newPosition = transform.pos;
            float3 newVelocity = linearVelocity;

            for( int i = 0; i < maxIterations; i++ )
            {
                MaxHitCollector<DistanceHit> distanceHitCollector = new MaxHitCollector<DistanceHit>( 0.0f, ref distanceHits );
                MaxHitCollector<ColliderCastHit> colliderHitCollector = new MaxHitCollector<ColliderCastHit>( 0.0f, ref colliderHits );
                int constraintCount = 0;

                // Distance Casts
                {
                    ColliderDistanceInput input = new ColliderDistanceInput
                    {
                        MaxDistance = 0.0f,
                        Transform = new RigidTransform
                        {
                            pos = newPosition,
                            rot = transform.rot
                        },
                        Collider = collider
                    };
                    world.CalculateDistance( input, ref distanceHitCollector );

                    for( int hitIndex = 0; hitIndex < distanceHitCollector.NumHits; hitIndex++ )
                    {
                        DistanceHit hit = distanceHitCollector.AllHits[hitIndex];
                        CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, hit.SurfaceNormal, hit.Distance, deltaTime, out SurfaceConstraintInfo constraint );
                        constraintInfos[ constraintCount++ ] = constraint;
                    }
                }
            }

            transform.pos = newPosition;
            linearVelocity = newVelocity;
        }
        
        public static void CreateConstraintFromHit( PhysicsWorld world, ColliderKey key, int rigidbodyIndex, float3 position, float3 normal, float distance, float deltaTime, out SurfaceConstraintInfo constraint )
        {
            constraint = new SurfaceConstraintInfo
            {
                Plane = new Plane
                {
                    Normal = normal,
                    Distance = distance
                },
                ColliderKey = key,
                RigidBodyIndex = rigidbodyIndex,
                HitPosition = position,
                Velocity = float3.zero,
                Priority = 1,
            };

            if( distance < 0.0f )
            {
                float3 recoveryVelocity = constraint.Velocity - constraint.Plane.Normal * distance;
                constraint.Velocity = recoveryVelocity;
            }
        }
    }
}
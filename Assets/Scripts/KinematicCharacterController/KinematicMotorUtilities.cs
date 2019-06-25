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

        public static unsafe void CollectDistanceCollisions( PhysicsWorld world, RigidTransform transform, Collider* collider, float skinWidth, float deltaTime, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<SurfaceConstraintInfo> constraintInfos )
        {
            MaxHitCollector<DistanceHit> distanceHitCollector = new KinematicMotorUtilities.MaxHitCollector<DistanceHit>( 0.0f, ref distanceHits );
            
            int numConstraints = 0;
            {
                ColliderDistanceInput distanceInput = new ColliderDistanceInput
                {
                    MaxDistance = skinWidth,
                    Transform = new RigidTransform
                    {
                        pos = transform.pos,
                        rot = transform.rot
                    },
                    Collider = collider
                };
                world.CalculateDistance( distanceInput, ref distanceHitCollector );

                for( int hitIndex = 0; hitIndex < distanceHitCollector.NumHits; hitIndex++ )
                {
                    DistanceHit hit = distanceHitCollector.AllHits[ hitIndex ];
                    CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, hit.SurfaceNormal, hit.Distance, deltaTime, out SurfaceConstraintInfo constraint );
                    constraintInfos[ numConstraints++ ] = constraint;
                }
            }

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
                Velocity = world.MotionVelocities[ rigidbodyIndex ].LinearVelocity,
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
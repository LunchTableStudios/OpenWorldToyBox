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

        public static unsafe int HandleMotorConstraints( PhysicsWorld world, Collider* collider, float skinWidth, float deltaTime, ref RigidTransform transform, ref float3 linearVelocity, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> colliderHits, ref NativeArray<SurfaceConstraintInfo> constraintInfos )
        {
            int numConstraints = 0;

            MaxHitCollector<DistanceHit> distanceHitCollector = new KinematicMotorUtilities.MaxHitCollector<DistanceHit>( 0.0f, ref distanceHits );
            MaxHitCollector<ColliderCastHit> colliderHitCollector = new KinematicMotorUtilities.MaxHitCollector<ColliderCastHit>( 1.0f, ref colliderHits );

            // Distance Detection
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

                for( int i = 0; i < distanceHitCollector.NumHits; i++ )
                {
                    DistanceHit hit = distanceHitCollector.AllHits[ i ];
                    CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, float3.zero, hit.Position, hit.SurfaceNormal, hit.Distance, deltaTime, out SurfaceConstraintInfo constraint );
                    constraintInfos[ numConstraints++ ] = constraint;
                }
            }

            // Collider Detection
            {
                float3 displacement = linearVelocity;

                ColliderCastInput colliderInput = new ColliderCastInput
                {
                    Position = transform.pos,
                    Direction = displacement,
                    Orientation = transform.rot,
                    Collider = collider
                };
                world.CastCollider( colliderInput, ref colliderHitCollector );

                for( int i = 0; i < colliderHitCollector.NumHits; i++ )
                {
                    ColliderCastHit hit = colliderHitCollector.AllHits[i];

                    bool duplicate = false;

                    for( int distanceHitIndex = 0; distanceHitIndex < distanceHitCollector.NumHits; distanceHitIndex++ )
                    {
                        DistanceHit distanceHit = distanceHitCollector.AllHits[ distanceHitIndex ];
                        if( distanceHit.RigidBodyIndex == hit.RigidBodyIndex && distanceHit.ColliderKey.Equals( hit.ColliderKey ) )
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    // Skip duplicate hits
                    if( !duplicate )
                    {
                        CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, float3.zero, hit.Position, hit.SurfaceNormal, hit.Fraction * math.length( displacement ), deltaTime, out SurfaceConstraintInfo constraint );
                        constraintInfos[ numConstraints++ ] = constraint;
                    }
                }
            }

            float3 newPostion = transform.pos;
            float3 newVelocity = linearVelocity;

            SimplexSolver.Solve( world, deltaTime, math.up(), numConstraints, ref constraintInfos, ref newPostion, ref newVelocity, out float integratedTime );

            transform.pos = newPostion;
            linearVelocity = newVelocity;

            return numConstraints;
        }
        
        public static void CreateConstraintFromHit( PhysicsWorld world, ColliderKey key, int rigidbodyIndex, float3 velocity, float3 position, float3 normal, float distance, float deltaTime, out SurfaceConstraintInfo constraint )
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
                Velocity = velocity,
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
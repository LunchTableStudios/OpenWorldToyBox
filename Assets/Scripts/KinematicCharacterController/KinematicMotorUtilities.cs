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

        public static void CreateConstraintFromHit( PhysicsWorld world, ColliderKey key, int rigidbodyIndex, float3 position, float3 velocity, float3 normal, float distance, float deltaTime, out SurfaceConstraintInfo constraint )
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
                Velocity = dynamicBody ? world.MotionVelocities[ rigidbodyIndex ].LinearVelocity : velocity
            };

            if( distance < 0.0f )
            {
                constraint.Velocity = constraint.Velocity - constraint.Plane.Normal * distance;
            }
        }

        public static void CreateSlopeConstraint( float3 up, float maxSlopCos, ref SurfaceConstraintInfo constraint, ref NativeArray<SurfaceConstraintInfo> constraints, ref int constraintCount )
        {
            float verticalDot = math.dot( constraint.Plane.Normal, up );
            bool validPlane = true;//verticalDot > SimplexSolver.c_SimplexSolverEpsilon && verticalDot < maxSlopCos;
            if( validPlane )
            {
                SurfaceConstraintInfo slopeConstraint = constraint;
                slopeConstraint.Plane.Normal = math.normalize( slopeConstraint.Plane.Normal - verticalDot * up );

                float distance = slopeConstraint.Plane.Distance;

                slopeConstraint.Plane.Distance = distance / math.dot( slopeConstraint.Plane.Normal, constraint.Plane.Normal );

                if( distance < 0.0f )
                {
                    constraint.Plane.Distance = 0.0f;

                    float3 newVelocity = slopeConstraint.Velocity - slopeConstraint.Plane.Normal * distance;
                    slopeConstraint.Velocity = newVelocity;
                }

                constraints[ constraintCount++ ] = slopeConstraint;
            }
        }

        public static unsafe void SolveCollisionConstraints( PhysicsWorld world, float deltaTime, int maxIterations, float skinWidth, float maxSlope, Collider* collider, ref RigidTransform transform, ref float3 velocity, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> colliderHits, ref NativeArray<SurfaceConstraintInfo> surfaceConstraints )
        {
            float remainingTime = deltaTime;
            float3 previousDisplacement = velocity * remainingTime;

            float3 outPosition = transform.pos;
            float3 outVelocity = velocity;

            quaternion orientation = transform.rot;

            const float timeEpsilon = 0.000001f;

            for( int i = 0; i < maxIterations && remainingTime > timeEpsilon; i++ )
            {
                MaxHitCollector<DistanceHit> distanceHitCollector = new MaxHitCollector<DistanceHit>( skinWidth, ref distanceHits );

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
                        CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, float3.zero, hit.SurfaceNormal, hit.Distance, deltaTime, out SurfaceConstraintInfo constraint );
                        CreateSlopeConstraint( math.up(), math.cos( maxSlope ), ref constraint, ref surfaceConstraints, ref constraintCount );
                        surfaceConstraints[ constraintCount++ ] = constraint;
                    }
                }

                // Handle Collider 
                {
                    float3 displacement = previousDisplacement;
                    MaxHitCollector<ColliderCastHit> colliderHitCollector = new MaxHitCollector<ColliderCastHit>( 1.0f, ref colliderHits );
                    
                    ColliderCastInput input = new ColliderCastInput
                    {
                        Collider = collider,
                        Position = outPosition,
                        Direction = velocity,
                        Orientation = orientation
                    };
                    world.CastCollider( input, ref colliderHitCollector );

                    for( int hitIndex = 0; hitIndex < colliderHitCollector.NumHits; hitIndex++ )
                    {
                        ColliderCastHit hit = colliderHitCollector.AllHits[ hitIndex ];

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

                        if( !duplicate )
                        {
                            CreateConstraintFromHit( world, hit.ColliderKey, hit.RigidBodyIndex, hit.Position, outVelocity, hit.SurfaceNormal, hit.Fraction * math.length( previousDisplacement ), deltaTime, out SurfaceConstraintInfo constraint );
                            CreateSlopeConstraint( math.up(), math.cos( maxSlope ), ref constraint, ref surfaceConstraints, ref constraintCount );
                            surfaceConstraints[ constraintCount++ ] = constraint;
                        }
                    }
                }

                float3 previousPosition = outPosition;
                float3 previousVelocity = outVelocity;

                SimplexSolver.Solve( world, remainingTime, math.up(), constraintCount, ref surfaceConstraints, ref outPosition, ref outVelocity, out float integratedTime );

                float3 currentDisplacement = outPosition - previousPosition;

                MaxHitCollector<ColliderCastHit> displacementHitCollector = new MaxHitCollector<ColliderCastHit>( 1.0f, ref colliderHits );
                int displacementContactIndex = -1;

                if( math.lengthsq( currentDisplacement ) > SimplexSolver.c_SimplexSolverEpsilon )
                {
                    ColliderCastInput input = new ColliderCastInput
                    {
                        Collider = collider,
                        Position = previousPosition,
                        Direction = currentDisplacement,
                        Orientation = orientation
                    };
                    world.CastCollider( input, ref displacementHitCollector );

                    for( int hitIndex = 0; hitIndex < distanceHitCollector.NumHits; hitIndex++ )
                    {
                        ColliderCastHit hit = displacementHitCollector.AllHits[ hitIndex ];

                        bool duplicate = false;
                        for( int constrainIndex = 0; constrainIndex < constraintCount; constrainIndex++ )
                        {
                            SurfaceConstraintInfo constraint = surfaceConstraints[ constrainIndex ];
                            if( constraint.RigidBodyIndex == hit.RigidBodyIndex && constraint.ColliderKey.Equals( hit.ColliderKey ) )
                            {
                                duplicate = true;
                                break;
                            }

                            if( !duplicate )
                            {
                                displacementContactIndex = hitIndex;
                                break;
                            }
                        }
                    }

                    if( displacementContactIndex >= 0 )
                    {
                        ColliderCastHit newContact = displacementHitCollector.AllHits[ displacementContactIndex ];

                        float fraction = newContact.Fraction / math.length( currentDisplacement );
                        integratedTime *= fraction;

                        float3 displacement = currentDisplacement * fraction;
                        outPosition = previousPosition + displacement;
                    }
                }

                remainingTime -= integratedTime;

                previousDisplacement = outVelocity * remainingTime;
            }

            transform.pos = outPosition;
            velocity = outVelocity;
        }
    }
}
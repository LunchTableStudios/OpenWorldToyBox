namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Mathematics;
    using Unity.Collections;

    public class KinematicMotorSystem : JobComponentSystem
    {
        public static unsafe void GetDistanceCollisions( PhysicsWorld world, RigidTransform transform, Collider* collider, float skinWidth, ref NativeArray<DistanceHit> distanceHits, ref NativeArray<SurfaceConstraintInfo> constraintInfos )
        {
            KinematicMotorUtilities.MaxHitCollector<DistanceHit> distanceHitCollector = new KinematicMotorUtilities.MaxHitCollector<DistanceHit>( 0.0f, ref distanceHits );
            
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
            }

        }

        protected override JobHandle OnUpdate(JobHandle inputDependencies )
        {
            return inputDependencies;
        }
    }
}
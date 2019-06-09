namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Collections;

    public class KinematicMovementSystem : JobComponentSystem
    {
        private struct KinematicMovementJob : IJobForEach<KinematicMovement, PhysicsVelocity>
        {
            public void Execute( [ ReadOnly ] ref KinematicMovement movement, ref PhysicsVelocity velocity )
            {
                velocity.Linear = movement.Value;
            }
        }

        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            KinematicMovementJob job = new KinematicMovementJob();

            return job.Schedule( this, inputDependencies );
        }
    }
}
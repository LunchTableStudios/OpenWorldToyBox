namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Collections;

    public class GravitySystem : JobComponentSystem
    {
        private struct GravityJob : IJobForEach<Gravity, KinematicMovement>
        {
            public float DeltaTime;

            public void Execute( [ ReadOnly ] ref Gravity gravity, ref KinematicMovement movement )
            {
                movement.Value += gravity.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            GravityJob job = new GravityJob{
                DeltaTime = UnityEngine.Time.deltaTime
            };

            return job.Schedule( this, inputDependencies );
        }
    }
}
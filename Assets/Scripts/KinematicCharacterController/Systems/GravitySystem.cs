namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Collections;

    public class GravitySystem : JobComponentSystem
    {
        private struct GravityJob : IJobForEach<Gravity, Movement>
        {
            public float deltaTime;

            public void Execute( [ ReadOnly ] ref Gravity gravity, ref Movement movement )
            {
                movement.Value += gravity.Value * deltaTime;
            }
        }

        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            GravityJob job = new GravityJob
            {
                deltaTime = Time.deltaTime
            };

            return job.Schedule( this, inputDependencies );
        }
    }
}
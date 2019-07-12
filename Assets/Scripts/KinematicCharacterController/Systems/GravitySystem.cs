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
            public float DeltaTime;

            public void Execute( [ ReadOnly ] ref Gravity gravity, ref Movement movement )
            {
                movement.Value += gravity.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            GravityJob job = new GravityJob
            {
                DeltaTime = Time.deltaTime
            };

            return job.Schedule( this, inputDependencies );
        }
    }
}
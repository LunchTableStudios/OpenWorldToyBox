namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;

    public class MovementSystem : JobComponentSystem
    {
        private struct MovementToDeltaJob : IJobForEach<Movement>
        {
            public float DeltaTime;

            public void Execute( ref Movement movement )
            {
                movement.Delta = movement.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            MovementToDeltaJob job = new MovementToDeltaJob
            {
                DeltaTime = UnityEngine.Time.deltaTime
            };

            return job.Schedule( this, inputDependencies );
        }
    }
}
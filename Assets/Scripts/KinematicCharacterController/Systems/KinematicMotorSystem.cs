namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Mathematics;
    using Unity.Collections;

    public class KinematicMotorSystem : JobComponentSystem
    {
        

        protected override JobHandle OnUpdate(JobHandle inputDependencies )
        {
            return inputDependencies;
        }
    }
}
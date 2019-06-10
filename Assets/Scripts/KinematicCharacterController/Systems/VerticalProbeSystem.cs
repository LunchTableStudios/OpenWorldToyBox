namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;

    public class VerticalProbeSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate( JobHandle inputDependencies )
        {
            return inputDependencies;
        }
    }
}
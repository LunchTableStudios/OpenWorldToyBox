namespace KinematicCharacterController
{
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Burst;
    using Unity.Physics;
    using Unity.Collections;

    public static class PhysicsHelpers
    {
        [ BurstCompile ]
        public struct ColliderCastJob : IJobParallelFor
        {
            [ ReadOnly ] public CollisionWorld World;
            [ ReadOnly ] public NativeArray<ColliderCastInput> Inputs;
            public NativeArray<ColliderCastHit> Hits;

            public void Execute( int i )
            {
                ColliderCastHit hit;
                World.CastCollider( Inputs[i], out hit );
                Hits[i] = hit;
            }
        }

        public static JobHandle ScheduleColliderCasts( CollisionWorld world, NativeArray<ColliderCastInput> inputs, NativeArray<ColliderCastHit> hits, int batchCount = 5 )
        {
            ColliderCastJob castJob = new ColliderCastJob
            {
                World = world,
                Inputs = inputs,
                Hits = hits
            };

            JobHandle handle = castJob.Schedule( inputs.Length, batchCount );

            return handle;
        }
    }
}
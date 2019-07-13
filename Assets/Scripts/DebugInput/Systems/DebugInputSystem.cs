namespace DebugInput
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    using KinematicCharacterController;
    
    public class DebugInputSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach( ( ref DebugInput input, ref KinematicCharacterController.CharacterController controller, ref Movement movement ) => {
                float xMovement = ( Input.GetKey( KeyCode.D ) ) ? 1 : ( Input.GetKey( KeyCode.A ) ) ? -1 : 0;
                float zMovement = ( Input.GetKey( KeyCode.W ) ) ? 1 : ( Input.GetKey( KeyCode.S ) ) ? -1 : 0;

                float3 strafeVector = xMovement * Camera.main.transform.right;
                strafeVector.y = 0;
                float3 forwardVector = zMovement * Camera.main.transform.forward;
                forwardVector.y = 0;

                float3 horizontalVector = math.normalizesafe( strafeVector + forwardVector );

                float3 normalizedInput = horizontalVector * controller.MaxSpeed * controller.Acceleration;
                
                
                controller.TargetDirection += normalizedInput;
                controller.TargetDirection.x = math.clamp( controller.TargetDirection.x, -controller.MaxSpeed, controller.MaxSpeed );
                controller.TargetDirection.z = math.clamp( controller.TargetDirection.z, -controller.MaxSpeed, controller.MaxSpeed );

                movement.Value.x = controller.TargetDirection.x;
                movement.Value.z = controller.TargetDirection.z;

                controller.TargetDirection *= controller.Friction;

            } );
        }
    }
}
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
                float3 forwardVector = zMovement * Camera.main.transform.forward;

                float3 horizontalVector = math.normalizesafe( strafeVector + forwardVector );
                horizontalVector.y = 0;

                float3 normalizedInput = horizontalVector * controller.MaxSpeed * controller.Acceleration;
                movement.Value += normalizedInput;
            } );
        }
    }
}
namespace DebugInput
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Mathematics;
    using KinematicCharacterController;

    [ UpdateBefore( typeof( MovementSystem ) ) ]
    public class DebugInputSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach( ( ref DebugInput input, ref Movement movement ) => {
                
                float xMovement = ( Input.GetKey( KeyCode.D ) ) ? 1 : ( Input.GetKey( KeyCode.A ) ) ? -1 : 0;
                float yMovement = ( Input.GetKey( KeyCode.Space ) ) ? 1 : ( Input.GetKey( KeyCode.LeftControl ) ) ? -1 : 0;
                float zMovement = ( Input.GetKey( KeyCode.W ) ) ? 1 : ( Input.GetKey( KeyCode.S ) ) ? -1 : 0;

                float3 strafeSpeed = xMovement * Camera.main.transform.right;
                float3 forwardSpeed = zMovement * Camera.main.transform.forward;

                float3 horizontalMovement = forwardSpeed + strafeSpeed;
                float3 verticalMovement = new float3( 0, yMovement, 0 );

                float3 inputMovement = horizontalMovement + verticalMovement;

                float3 normalizedInput = math.normalizesafe( inputMovement ) * input.Speed;
                movement.Value = normalizedInput;

            } );
        }
    }
}
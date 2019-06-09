namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public class KinematicMotorBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public KinematicMotor Data;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, Data );
            manager.AddComponentData( entity, new KinematicMovement() );
        }
    }
}
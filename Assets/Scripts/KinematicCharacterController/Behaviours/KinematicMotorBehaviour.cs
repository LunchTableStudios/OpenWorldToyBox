namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public class KinematicMotorBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public KinematicMotor Motor;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, Motor );
        }
    }
}
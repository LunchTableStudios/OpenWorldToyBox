namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public class CharacterControllerBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public CharacterController ControllerSettings;
        public Gravity GravitySettings;
        public KinematicMotor MotorSettings;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, ControllerSettings );
            manager.AddComponentData( entity, GravitySettings );
            manager.AddComponentData( entity, MotorSettings );
            
            manager.AddComponentData( entity, new Movement() );
        }
    }
}
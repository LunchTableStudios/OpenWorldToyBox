namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public class KinematicMotorBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public KinematicMotor MotorSettings;
        public Gravity GravitySettings;

        private Movement m_movement;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, MotorSettings );
            manager.AddComponentData( entity, GravitySettings );
            manager.AddComponentData( entity, m_movement );
        }
    }
}
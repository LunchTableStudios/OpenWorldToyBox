namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public class GravityBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Gravity Data;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, Data );
        }
    }
}
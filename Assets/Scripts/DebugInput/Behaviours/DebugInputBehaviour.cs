namespace DebugInput
{
    using UnityEngine;
    using Unity.Entities;

    public class DebugInputBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, new DebugInput{ Speed = 8 } );
        }
    }
}
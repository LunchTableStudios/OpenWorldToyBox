namespace DebugInput
{
    using UnityEngine;
    using Unity.Entities;

    public class DebugInputBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        public DebugInput Input;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            manager.AddComponentData( entity, Input );
        }
    }
}
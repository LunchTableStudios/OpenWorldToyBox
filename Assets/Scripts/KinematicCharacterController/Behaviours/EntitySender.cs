namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;

    public interface IRecieveEntity
    {
        void SetRecievedEntity( Entity entity );
    }

    public class EntitySender : MonoBehaviour, IConvertGameObjectToEntity
    {
        public GameObject entityReciever;

        public void Convert( Entity entity, EntityManager manager, GameObjectConversionSystem conversionSystem )
        {
            if( entityReciever == null ) 
                return;

            IRecieveEntity reciever = entityReciever.GetComponent<IRecieveEntity>();

            if( reciever != null )
                reciever.SetRecievedEntity( entity );     
        }
    }
}
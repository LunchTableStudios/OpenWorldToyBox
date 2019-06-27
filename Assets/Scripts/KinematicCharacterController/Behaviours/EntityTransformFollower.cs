namespace KinematicCharacterController
{
    using UnityEngine;
    using Unity.Entities;
    using Unity.Transforms;

    public class EntityTransformFollower : MonoBehaviour, IRecieveEntity
    {
        private Entity m_recievedEntity = Entity.Null;
        public Entity RecievedEntity
        {
            get
            {
                return m_recievedEntity;
            }
        }

        public void SetRecievedEntity( Entity entity )
        {
            m_recievedEntity = entity;
        }

        void LateUpdate()
        {
            if( m_recievedEntity != Entity.Null )
            {
                try
                {
                    EntityManager entityManager = World.Active.EntityManager;

                    transform.position = entityManager.GetComponentData<Translation>( m_recievedEntity ).Value;
                    transform.rotation = entityManager.GetComponentData<Rotation>( m_recievedEntity ).Value;
                }
                catch
                {
                    m_recievedEntity = Entity.Null;
                }
            }
        }
    }
}
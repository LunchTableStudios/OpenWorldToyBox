namespace KinematicCharacterController
{
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Physics.Systems;
    using Unity.Mathematics;
    using Unity.Collections;

    public unsafe class KinematicMotorQueryDebugger : MonoBehaviour
    {
        private EntityTransformFollower entityFollower;
        private Entity followedEntity;
        private bool m_simulating = false;
        private Dictionary<PrimitiveType, UnityEngine.Mesh> m_primitiveMeshes;

        void Start()
        {
            GatherPrimitiveMeshes();

            entityFollower = GetComponent<EntityTransformFollower>();

            m_simulating = true;
        }

        void OnDrawGizmos()
        {
            if( !m_simulating ) 
                return;

            // DrawPositionGizmo();
        }

        void OnDestroy()
        {
            
        }
        
        #region Gizmo Helpers
        private void GatherPrimitiveMeshes()
        {
            m_primitiveMeshes = new Dictionary<PrimitiveType, UnityEngine.Mesh>();

            foreach( PrimitiveType primitiveType in System.Enum.GetValues( typeof( PrimitiveType ) ) )
            {
                GameObject primitive = GameObject.CreatePrimitive( primitiveType );
                UnityEngine.Mesh mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate( primitive );
                m_primitiveMeshes.Add( primitiveType, mesh );
            }
        }

        private void DrawPositionGizmo()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], transform.position, transform.rotation );
        }
        #endregion
    }
}
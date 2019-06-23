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
        [ System.Serializable ]
        public class GizmoRenderSettings
        {
        }

        public GizmoRenderSettings Settings;
        public float3 InputMovement;
        public float SkinWidth = 0.03f;

        private bool m_simulating = false;

        private Dictionary<PrimitiveType, UnityEngine.Mesh> m_primitiveMeshes;
        private BlobAssetReference<Unity.Physics.Collider> m_collider;

        private float3 m_deltaInput
        {
            get
            {
                return InputMovement * Time.fixedDeltaTime;
            }
        }

        void Start()
        {
            GatherPrimitiveMeshes();

            m_simulating = true;

            m_collider = CreateCollider( m_primitiveMeshes[ PrimitiveType.Capsule ] );
        }

        void Update()
        {
            RunSimulation();
        }

        void OnDrawGizmos()
        {
            if( !m_simulating ) 
                return;

            DrawPositionGizmo();
        }

        void OnDestroy()
        {
            
        }

        private BlobAssetReference<Unity.Physics.Collider> CreateCollider( UnityEngine.Mesh mesh ) 
        {
            Bounds bounds = mesh.bounds;
            float min = math.cmin(bounds.extents);
            float max = math.cmax(bounds.extents);
            int x = math.select(math.select(2, 1, min == bounds.extents.y), 0, min == bounds.extents.x);
            int z = math.select(math.select(2, 1, max == bounds.extents.y), 0, max == bounds.extents.x);
            int y = math.select(math.select(2, 1, (1 != x) && (1 != z)), 0, (0 != x) && (0 != z));
            float radius = bounds.extents[y];
            float3 vertex0 = bounds.center; vertex0[z] = -(max - radius);
            float3 vertex1 = bounds.center; vertex1[z] = (max - radius);
            return Unity.Physics.CapsuleCollider.Create(vertex0, vertex1, radius);
        }

        private void RunSimulation()
        {
            ref PhysicsWorld world = ref World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
            
            ClearPreviousSimulationCollections();

            RigidTransform rigidTransform = new RigidTransform( transform.rotation.y, transform.position );
            
                        
        }

        private void ClearPreviousSimulationCollections()
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
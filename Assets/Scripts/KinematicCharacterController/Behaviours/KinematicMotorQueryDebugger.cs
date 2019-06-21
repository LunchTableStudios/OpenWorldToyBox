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
            public bool ShowPosition = true;
            public bool ShowDistance = true;
            public bool ShowVelocity = true;
            public bool ShowCollisions = true;
        }

        public GizmoRenderSettings Settings;
        public float3 InputMovement;
        public float SkinWidth = 0.03f;

        private bool m_simulating = false;

        private Dictionary<PrimitiveType, UnityEngine.Mesh> m_primitiveMeshes;
        private BlobAssetReference<Unity.Physics.Collider> m_collider;

        private NativeList<DistanceHit> m_distanceHits;
        private NativeList<ColliderCastHit> m_colliderHits;

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

            m_distanceHits = new NativeList<DistanceHit>( Allocator.Persistent );
            m_colliderHits = new NativeList<ColliderCastHit>( Allocator.Persistent );
        }

        void Update()
        {
            RunSimulation();
        }

        void OnDrawGizmos()
        {
            if( !m_simulating ) 
                return;
            
            if( Settings.ShowPosition )
                DrawPositionGizmo();

            if( Settings.ShowDistance )
                DrawDistanceHitGizmos();

            if( Settings.ShowVelocity )
                DrawVelocityGizmos();

            if( Settings.ShowCollisions )
                DrawColliderHitGizmos();
        }

        void OnDestroy()
        {
            if( m_distanceHits.IsCreated )
                m_distanceHits.Dispose();

            if( m_colliderHits.IsCreated )
                m_colliderHits.Dispose();
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
            
            SimulateDistanceQuery( ref world, rigidTransform );

            if( InputMovement.y != 0 )
                SimulateVerticalProbe( ref world, rigidTransform );
        }

        private void SimulateDistanceQuery( ref PhysicsWorld world, RigidTransform rigidTransform )
        {
            ColliderDistanceInput distanceInput = new ColliderDistanceInput
            {
                Collider = ( Unity.Physics.Collider* )m_collider.GetUnsafePtr(),
                Transform = rigidTransform,
                MaxDistance = SkinWidth
            };

            world.CalculateDistance( distanceInput, ref m_distanceHits );
        }

        private void SimulateVerticalProbe( ref PhysicsWorld world, RigidTransform rigidTransform )
        {
            ColliderCastInput colliderInput = new ColliderCastInput
            {
                Collider = ( Unity.Physics.Collider* )m_collider.GetUnsafePtr(),
                Direction = m_deltaInput,
                Position = rigidTransform.pos,
                Orientation = rigidTransform.rot                
            };

            world.CastCollider( colliderInput, ref m_colliderHits );
        }

        private void ClearPreviousSimulationCollections()
        {
            if( m_distanceHits.IsCreated )
                m_distanceHits.Clear();

            if( m_colliderHits.IsCreated )
                m_colliderHits.Clear();
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

        private void DrawDistanceHitGizmos()
        {
            if( !m_distanceHits.IsCreated )
                return;

            Gizmos.color = new Color( 255, 149, 0 );
            foreach( DistanceHit hit in m_distanceHits.ToArray() )
            {
                float3 queryPoint = hit.Position + hit.SurfaceNormal * hit.Distance;
                Gizmos.DrawWireSphere( hit.Position, 0.05f );
                Gizmos.DrawWireSphere( queryPoint, 0.05f );
                Gizmos.DrawLine( hit.Position, queryPoint );
            }
        }

        private void DrawColliderHitGizmos()
        {
            if( !m_colliderHits.IsCreated )
                return;

            Gizmos.color = Color.red;
            foreach( ColliderCastHit hit in m_colliderHits.ToArray() )
            {
                Gizmos.DrawRay( hit.Position, hit.SurfaceNormal );
            }
        }

        private void DrawVelocityGizmos()
        {
            Gizmos.color = Color.green;

            if( m_deltaInput.y != 0 )
            {
                Gizmos.DrawLine( transform.position, transform.position + new Vector3( 0, m_deltaInput.y, 0 ) );
                Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], transform.position + new Vector3( 0, m_deltaInput.y, 0 ), transform.rotation );
            }
            
            if( m_deltaInput.x != 0 || m_deltaInput.z != 0 )
            {
                Gizmos.DrawLine( transform.position, transform.position + new Vector3( m_deltaInput.x, 0, m_deltaInput.z ) );
                Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], transform.position + new Vector3( m_deltaInput.x, 0, m_deltaInput.z ), transform.rotation );
            }
        }
        #endregion
    }
}
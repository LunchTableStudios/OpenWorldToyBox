namespace KinematicCharacterController
{
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Transforms;
    using Unity.Collections;

    [ ExecuteInEditMode ]
    public class KinematicMotorDebugger : MonoBehaviour
    {
        private Dictionary<PrimitiveType, Mesh> m_primitiveMeshes;

        private EntityManager m_entityManager;
        private EntityQuery m_motorQuery;

        void Awake()
        {
            GatherPrimitiveMeshes();

            if( World.Active != null )
            {
                m_entityManager = World.Active.EntityManager;
                m_motorQuery = m_entityManager.CreateEntityQuery(
                    typeof( KinematicMotor ),
                    typeof( KinematicMovement ),
                    typeof( Translation )
                );
            }
        }

        void OnDrawGizmos()
        {
            if( m_primitiveMeshes == null || m_entityManager == null || m_motorQuery == null )
                return;

            DrawPositionGizmos();
            DrawHorizontalMovementGizmos();
            DrawVerticalMovementGizmos();
        }

        private void GatherPrimitiveMeshes()
        {
            m_primitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

            foreach( PrimitiveType primitiveType in System.Enum.GetValues( typeof( PrimitiveType ) ) )
            {
                GameObject primitive = GameObject.CreatePrimitive( primitiveType );
                Mesh mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                GameObject.DestroyImmediate( primitive );
                m_primitiveMeshes.Add( primitiveType, mesh );
            }
        }

        private void DrawPositionGizmos()
        {
            NativeArray<Translation> translations = m_motorQuery.ToComponentDataArray<Translation>( Allocator.TempJob );

            Gizmos.color = Color.magenta;
            foreach( Translation translation in translations )
            {
                Vector3 gizmoPosition = new Vector3( translation.Value.x, translation.Value.y, translation.Value.z );
                Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], gizmoPosition, Quaternion.identity );
            }

            translations.Dispose();
        }

        private void DrawHorizontalMovementGizmos()
        {
            NativeArray<Translation> translations = m_motorQuery.ToComponentDataArray<Translation>( Allocator.TempJob );
            NativeArray<KinematicMovement> movements = m_motorQuery.ToComponentDataArray<KinematicMovement>( Allocator.TempJob );

            Gizmos.color = new Color( 1, 0, 1 );
            for( int i = 0; i < translations.Length; i++ )
            {
                if( movements[i].Value.x != 0 || movements[i].Value.z != 0 )
                {
                    Vector3 origin = new Vector3( translations[i].Value.x, translations[i].Value.y, translations[i].Value.z );
                    Vector3 gizmoPosition = new Vector3( translations[i].Value.x, translations[i].Value.y, translations[i].Value.z );
                    gizmoPosition.x += movements[i].Value.x;
                    gizmoPosition.z += movements[i].Value.z;

                    Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], gizmoPosition, Quaternion.identity );
                    Gizmos.color = new Color( 1, 0, 1, 0.25f );
                    Gizmos.DrawLine( origin, gizmoPosition );
                }
            }

            translations.Dispose();
            movements.Dispose();
        }

        private void DrawVerticalMovementGizmos()
        {
            NativeArray<Translation> translations = m_motorQuery.ToComponentDataArray<Translation>( Allocator.TempJob );
            NativeArray<KinematicMovement> movements = m_motorQuery.ToComponentDataArray<KinematicMovement>( Allocator.TempJob );

            Gizmos.color = Color.green;
            for( int i = 0; i < translations.Length; i++ )
            {
                if( movements[i].Value.y != 0 )
                {
                    Vector3 origin = new Vector3( translations[i].Value.x, translations[i].Value.y, translations[i].Value.z );
                    Vector3 gizmoPosition = new Vector3( translations[i].Value.x, translations[i].Value.y, translations[i].Value.z );
                    gizmoPosition.y += movements[i].Value.y;

                    Gizmos.DrawWireMesh( m_primitiveMeshes[ PrimitiveType.Capsule ], gizmoPosition, Quaternion.identity );
                    Gizmos.color = new Color( 0, 1, 0, 0.25f );
                    Gizmos.DrawLine( origin, gizmoPosition );
                }
            }

            translations.Dispose();
            movements.Dispose();
        }
    }
}
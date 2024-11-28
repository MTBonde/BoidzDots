using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// Authoring class to define adjustable settings for boid behavior, such as weights and speeds.
    /// Allows runtime adjustments via the Unity Inspector.
    /// Disaalowmultiple tag prevent multiple instances.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoidSettingsAuthoring : MonoBehaviour
    {
        // Neighbor settings
        public float NeighborRadius = 5f;
        public int MaxNeighbors = 10;
        public int SpatialCellSize = 10;
        
        // Boid behavior settings
        public float MoveSpeed = 5f;
        public float AlignmentWeight = 1f;
        public float CohesionWeight = 1f;
        public float SeparationWeight = 1f;
        
        // Boundary settings
        public Vector3 BoundaryCenter = Vector3.zero;
        public float BoundarySize = 50f; 
        public float BoundaryWeight = 10f; 

        private Entity boidSettingsEntity;
        private EntityManager entityManager;

        void Awake()
        {
            // Get the EntityManager and singleton entity
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            boidSettingsEntity = entityManager.CreateEntityQuery(typeof(BoidSettings)).GetSingletonEntity();
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (boidSettingsEntity != Entity.Null)
            {
                BoidSettings boidSettings = entityManager.GetComponentData<BoidSettings>(boidSettingsEntity);
                boidSettings.NeighborRadius = NeighborRadius;
                boidSettings.MaxNeighbors = MaxNeighbors;
                boidSettings.SpatialCellSize = SpatialCellSize;
                boidSettings.MoveSpeed = MoveSpeed;
                boidSettings.AlignmentWeight = AlignmentWeight;
                boidSettings.CohesionWeight = CohesionWeight;
                boidSettings.SeparationWeight = SeparationWeight;
                boidSettings.BoundaryCenter = BoundaryCenter;
                boidSettings.BoundarySize = BoundarySize;
                boidSettings.BoundaryWeight = BoundaryWeight;
                entityManager.SetComponentData(boidSettingsEntity, boidSettings);
            }
        }

        private class BoidSettingsBaker : Baker<BoidSettingsAuthoring>
        {
            public override void Bake(BoidSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSettings
                {
                    NeighborRadius = authoring.NeighborRadius,
                    MaxNeighbors = authoring.MaxNeighbors,
                    SpatialCellSize = authoring.SpatialCellSize,
                    MoveSpeed = authoring.MoveSpeed,
                    AlignmentWeight = authoring.AlignmentWeight,
                    CohesionWeight = authoring.CohesionWeight,
                    SeparationWeight = authoring.SeparationWeight,
                    BoundaryCenter = authoring.BoundaryCenter,
                    BoundarySize = authoring.BoundarySize,
                    BoundaryWeight = authoring.BoundaryWeight
                });
            }
        }
    }

    /// <summary>
    /// Data component for boid behavior settings.
    /// </summary>
    public struct BoidSettings : IComponentData
    {
        // neighbor settings
        public float NeighborRadius;
        public int MaxNeighbors;
        public int SpatialCellSize;
        
        // boid behavior settings
        public float MoveSpeed;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float SeparationWeight;
        
        // Boundary parameters
        public float3 BoundaryCenter;
        public float BoundarySize; 
        public float BoundaryWeight;
    }
}
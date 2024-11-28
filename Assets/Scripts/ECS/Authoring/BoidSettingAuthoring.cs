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
        [Header("Neighbor Settings")]
        public float NeighborRadius = 5f;
        public int MaxNeighbors = 10;
        public int SpatialCellSize = 10;

        // Boid behavior settings
        [Header("Boid Behavior Settings")]
        public float MoveSpeed = 5f;
        public float AlignmentWeight = 1f;
        public float CohesionWeight = 1f;
        public float SeparationWeight = 1f;

        // Boundary settings
        [Header("Boundary Settings")] 
        public BoundaryType BoundaryType = BoundaryType.Sphere;
        public Vector3 BoundaryCenter = Vector3.zero;
        public float BoundarySize = 50f; 
        public float BoundaryWeight = 10f;


        private Entity _boidSettingsEntity;
        private EntityManager _entityManager;

        private void Awake()
        {
            // Get the EntityManager and singleton entity
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _boidSettingsEntity = _entityManager.CreateEntityQuery(typeof(BoidSettings)).GetSingletonEntity();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            if (_boidSettingsEntity != Entity.Null)
            {
                // Get the BoidSettings component from the singleton entity
                BoidSettings boidSettings = _entityManager.GetComponentData<BoidSettings>(_boidSettingsEntity);
                
                // Neighbor settings
                boidSettings.NeighborRadius = NeighborRadius;
                boidSettings.MaxNeighbors = MaxNeighbors;
                boidSettings.SpatialCellSize = SpatialCellSize;
                
                // Boid behavior settings
                boidSettings.MoveSpeed = MoveSpeed;
                boidSettings.AlignmentWeight = AlignmentWeight;
                boidSettings.CohesionWeight = CohesionWeight;
                boidSettings.SeparationWeight = SeparationWeight;
                
                // Boundary settings
                boidSettings.BoundaryType = BoundaryType;
                boidSettings.BoundaryCenter = BoundaryCenter;
                boidSettings.BoundarySize = BoundarySize;
                boidSettings.BoundaryWeight = BoundaryWeight;
                
                // Update the singleton entity
                _entityManager.SetComponentData(_boidSettingsEntity, boidSettings);
            }
        }

        private class BoidSettingsBaker : Baker<BoidSettingsAuthoring>
        {
            public override void Bake(BoidSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSettings
                {
                    // Neighbor settings
                    NeighborRadius = authoring.NeighborRadius,
                    MaxNeighbors = authoring.MaxNeighbors,
                    SpatialCellSize = authoring.SpatialCellSize,
                    
                    // Boid behavior settings
                    MoveSpeed = authoring.MoveSpeed,
                    AlignmentWeight = authoring.AlignmentWeight,
                    CohesionWeight = authoring.CohesionWeight,
                    SeparationWeight = authoring.SeparationWeight,
                    
                    // Boundary settings
                    BoundaryType = authoring.BoundaryType,
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
        public BoundaryType BoundaryType;
        public float3 BoundaryCenter;
        public float BoundarySize; 
        public float BoundaryWeight;
    }
    
    public enum BoundaryType
    {
        Sphere,
        Box,
        Donut
    }
}
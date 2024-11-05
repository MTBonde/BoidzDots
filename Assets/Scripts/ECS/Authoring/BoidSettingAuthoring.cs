using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// Authoring class to define adjustable settings for boid behavior, such as weights and speeds.
    /// Allows runtime adjustments via the Unity Inspector.
    /// </summary>
    public class BoidSettingsAuthoring : MonoBehaviour
    {
        public float NeighborRadius = 5f;
        public float MoveSpeed = 5f;
        public float AlignmentWeight = 1f;
        public float CohesionWeight = 1f;
        public float SeparationWeight = 1f;

        private class BoidSettingsBaker : Baker<BoidSettingsAuthoring>
        {
            public override void Bake(BoidSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSettings
                {
                    NeighborRadius = authoring.NeighborRadius,
                    MoveSpeed = authoring.MoveSpeed,
                    AlignmentWeight = authoring.AlignmentWeight,
                    CohesionWeight = authoring.CohesionWeight,
                    SeparationWeight = authoring.SeparationWeight
                });
            }
        }
    }

    /// <summary>
    /// Data component for boid behavior settings.
    /// </summary>
    public struct BoidSettings : IComponentData
    {
        public float NeighborRadius;
        public float MoveSpeed;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float SeparationWeight;
    }
}
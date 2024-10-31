using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// Authoring class to define settings for boid spawning, such as the boid count.
    /// This class makes boid settings accessible in the Unity Inspector.
    /// </summary>
    public class BoidSpawnerAuthoring : MonoBehaviour
    {
        // Set the number of boids to spawn in the Inspector.
        public int MaxBoidCount = 1000;
        
        private class BoidSettingsBaker : Baker<BoidSpawnerAuthoring>
        {
            public override void Bake(BoidSpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new BoidSpawner { MaxBoidCount = authoring.MaxBoidCount });
            }
        }
    }

    /// <summary>
    /// Data component for boid spawning settings, such as the boid count.
    /// </summary>
    public struct BoidSpawner : IComponentData
    {
        public int MaxBoidCount;
    }
}
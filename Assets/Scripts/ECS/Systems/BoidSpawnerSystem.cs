using ECS.Authoring;
using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// BoidSpawnerSystem is responsible for spawning boids.
    /// It ensures the number of spawned boids doesn't exceed the specified maximum count,
    /// and initializes each boid's speed, direction, and position within a random range.
    /// </summary>
    public partial struct BoidSpawnerSystem : ISystem
    {
        // Persistent boid count tracker
        public int CurrentBoidCount; 
        
        /// <summary>
        /// Initializes the BoidSpawnerSystem by requiring the EntitiesReferences singleton and BoidSpawner components.
        /// ensuring that OnUpdate only runs when entities with these components exist.
        /// </summary>
        /// <param name="state"></param>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<BoidSpawner>();
        }

        /// <summary>
        /// Spawns boids if the current boid count is below the maximum specified count.
        /// Initializes each boid's speed, direction, and position within a random range.
        /// </summary>
        /// <param name="state">The state of the system.</param>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Retrieve references for boid prefab and spawner settings
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();
            Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            // Iterate through all enteties with boid spawner components, set read-only
            foreach ((RefRO<LocalTransform> localTransform, RefRO<BoidSpawner> boidSpawner) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<BoidSpawner>>())
            {
                // EO; If current boid count exceeds max count
                if (CurrentBoidCount >= boidSpawner.ValueRO.MaxBoidCount) return;
    
                // Spawn boids up to the specified MaxBoidCount
                int spawnAmount = boidSpawner.ValueRO.MaxBoidCount - CurrentBoidCount;
                for (int i = 0; i < spawnAmount; i++)
                {
                    Entity boidEntity = state.EntityManager.Instantiate(entitiesReferences.BoidPrefabEntity);

                    // Set random spawn position relative to the spawner's position
                    float3 positionOffset = random.NextFloat3(-10f, 10f);
                    float3 spawnPosition = localTransform.ValueRO.Position + positionOffset;
                    state.EntityManager.SetComponentData(boidEntity, LocalTransform.FromPosition(spawnPosition));

                    // Generate a random target point near the boundary center
                    float3 targetOffset = random.NextFloat3(-10f, 10f);
                    float3 boundaryCenter = boidSettings.BoundaryCenter;
                    float3 targetPosition = boundaryCenter + targetOffset;
                    
                    // Calculate the direction from spawn position to the target point
                    float3 direction = math.normalize(targetPosition - spawnPosition);

                    // Set the DirectionComponent
                    state.EntityManager.SetComponentData(boidEntity, new DirectionComponent { Direction = direction });

                    // Set the MoveSpeedComponent
                    float speed = random.NextFloat(1f, 3f);

                    CurrentBoidCount++;
                }
            }

            // OLD CODE
            
            // // Iterate through all boid spawners
            // foreach ((RefRO<LocalTransform> localTransform, RefRO<BoidSpawner> boidSpawner) 
            //          in SystemAPI.Query<RefRO<LocalTransform>, RefRO<BoidSpawner>>())
            // {
            //     // Check if current boid count exceeds max count
            //     if (CurrentBoidCount >= boidSpawner.ValueRO.MaxBoidCount)
            //         return;
            //
            //     // Spawn boids up to the specified MaxBoidCount
            //     int spawnAmount = boidSpawner.ValueRO.MaxBoidCount - CurrentBoidCount;
            //
            //     // Spawn boids with random speed, direction, and position
            //     for (int i = 0; i < spawnAmount; i++)
            //     {
            //         // Instantiate a boid entity
            //         Entity boidEntity = state.EntityManager.Instantiate(entitiesReferences.BoidPrefabEntity);
            //
            //         // Set random speed, direction, and position within a spawn range
            //         float randomSpeed = random.NextFloat(1f, 3f);
            //         float3 randomDirection = math.normalize(random.NextFloat3Direction());
            //         float3 randomOffset = random.NextFloat3(-10f, 10f); // Offset within a 10-unit range
            //         float speed = state.EntityManager.GetComponentData<MoveSpeedComponent>(boidEntity).Speed;
            //         state.EntityManager.SetComponentData(boidEntity, new MoveSpeedComponent { Speed = randomSpeed + speed });
            //         state.EntityManager.SetComponentData(boidEntity, new DirectionComponent { Direction = randomDirection });
            //         
            //         // Set boid position relative to the spawner's position
            //         float3 spawnPosition = localTransform.ValueRO.Position + randomOffset;
            //         state.EntityManager.SetComponentData(boidEntity, LocalTransform.FromPosition(spawnPosition));
            //
            //         // Increment the boid count
            //         CurrentBoidCount++;
            //     }
            // }
        }
    }
}

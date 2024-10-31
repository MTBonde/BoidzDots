using ECS.Authoring;
using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public partial struct BoidSpawnerSystem : ISystem
    {
        // Persistent boid count tracker
        public int CurrentBoidCount; 
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesReferences>();
            state.RequireForUpdate<BoidSpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Retrieve references for boid prefab and spawner settings
            EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
            Random random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

            foreach ((RefRO<LocalTransform> localTransform, RefRO<BoidSpawner> boidSpawner) 
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<BoidSpawner>>())
            {
                // Check if current boid count exceeds max count
                if (CurrentBoidCount >= boidSpawner.ValueRO.MaxBoidCount)
                    return;

                // Spawn boids up to the specified MaxBoidCount
                int spawnAmount = boidSpawner.ValueRO.MaxBoidCount - CurrentBoidCount;

                for (int i = 0; i < spawnAmount; i++)
                {
                    Entity boidEntity = state.EntityManager.Instantiate(entitiesReferences.BoidPrefabEntity);

                    // Set random speed, direction, and position within a spawn range
                    float randomSpeed = random.NextFloat(1f, 3f);
                    float3 randomDirection = math.normalize(random.NextFloat3Direction());
                    float3 randomOffset = random.NextFloat3(-10f, 10f); // Offset within a 10-unit range
                    
                    state.EntityManager.SetComponentData(boidEntity, new MoveSpeedComponent { Speed = randomSpeed });
                    state.EntityManager.SetComponentData(boidEntity, new DirectionComponent { Direction = randomDirection });
                    
                    // Set boid position relative to the spawner's position
                    float3 spawnPosition = localTransform.ValueRO.Position + randomOffset;
                    state.EntityManager.SetComponentData(boidEntity, LocalTransform.FromPosition(spawnPosition));

                    CurrentBoidCount++;
                }
            }
        }
    }
}

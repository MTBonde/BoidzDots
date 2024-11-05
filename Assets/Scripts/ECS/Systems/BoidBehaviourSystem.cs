using ECS.Authoring;
using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public struct BoidData
    {
        public float3 Position;
        public float3 Velocity;
    }
    
    [BurstCompile]
    public partial struct BoidBehaviorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Retrieve BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();

            // Get boid query
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<BoidTag, LocalTransform, VelocityComponent>()
                .Build();

            // Get arrays of positions and velocities
            int boidCount = boidQuery.CalculateEntityCount();

            NativeArray<BoidData> boidDataArray = new NativeArray<BoidData>(boidCount, Allocator.TempJob);
            NativeArray<Entity> entities = boidQuery.ToEntityArray(Allocator.TempJob);

            // Collect data
            var collectJob = new CollectBoidDataJob
            {
                BoidDataArray = boidDataArray
            };
            state.Dependency = collectJob.ScheduleParallel(boidQuery, state.Dependency);

            state.Dependency.Complete();

            // Create the boid behavior job
            var boidBehaviorJob = new BoidBehaviorJob
            {
                DeltaTime = deltaTime,
                BoidSettings = boidSettings,
                BoidDataArray = boidDataArray,
                Entities = entities,
                LocalTransformLookup = state.GetComponentLookup<LocalTransform>(false),
                VelocityLookup = state.GetComponentLookup<VelocityComponent>(false),
            };

            // Schedule the job
            state.Dependency = boidBehaviorJob.Schedule(boidCount, 64, state.Dependency);

            state.Dependency.Complete();

            boidDataArray.Dispose();
            entities.Dispose();
        }

        [BurstCompile]
        public partial struct CollectBoidDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<BoidData> BoidDataArray;

            public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in VelocityComponent velocity)
            {
                BoidDataArray[index] = new BoidData
                {
                    Position = transform.Position,
                    Velocity = velocity.Velocity
                };
            }
        }

        [BurstCompile]
        public struct BoidBehaviorJob : IJobParallelFor
        {
            public float DeltaTime;
            public BoidSettings BoidSettings;

            [ReadOnly] public NativeArray<BoidData> BoidDataArray;
            [ReadOnly] public NativeArray<Entity> Entities;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<VelocityComponent> VelocityLookup;

            public void Execute(int index)
            {
                BoidData currentBoid = BoidDataArray[index];
                float3 currentPosition = currentBoid.Position;
                float3 currentVelocity = currentBoid.Velocity;

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;
                int neighborCount = 0;

                // Loop over all boids to find neighbors (naive approach)
                for (int i = 0; i < BoidDataArray.Length; i++)
                {
                    if (i == index) continue;

                    float3 neighborPosition = BoidDataArray[i].Position;
                    float3 neighborVelocity = BoidDataArray[i].Velocity;

                    float distance = math.distance(currentPosition, neighborPosition);

                    if (distance > 0 && distance < BoidSettings.NeighborRadius)
                    {
                        alignment += neighborVelocity;
                        cohesion += neighborPosition;
                        separation += (currentPosition - neighborPosition) / distance;
                        neighborCount++;
                    }
                }

                if (neighborCount > 0)
                {
                    alignment = alignment / neighborCount;
                    alignment = math.normalize(alignment) * BoidSettings.AlignmentWeight;

                    cohesion = (cohesion / neighborCount) - currentPosition;
                    cohesion = math.normalize(cohesion) * BoidSettings.CohesionWeight;

                    separation = separation / neighborCount;
                    separation = math.normalize(separation) * BoidSettings.SeparationWeight;
                }

                // Calculate acceleration
                float3 acceleration = alignment + cohesion + separation;

                // Update velocity
                currentVelocity += acceleration * DeltaTime;

                // Limit speed
                float speed = math.length(currentVelocity);
                if (speed > BoidSettings.MoveSpeed)
                {
                    currentVelocity = (currentVelocity / speed) * BoidSettings.MoveSpeed;
                }

                // Update position
                currentPosition += currentVelocity * DeltaTime;

                // Boundary checking
                float3 minBounds = BoidSettings.BoundaryCenter - BoidSettings.BoundarySize;
                float3 maxBounds = BoidSettings.BoundaryCenter + BoidSettings.BoundarySize;

                float3 steer = float3.zero;

                if (currentPosition.x < minBounds.x)
                {
                    steer.x = BoidSettings.BoundaryWeight;
                }
                else if (currentPosition.x > maxBounds.x)
                {
                    steer.x = -BoidSettings.BoundaryWeight;
                }

                if (currentPosition.y < minBounds.y)
                {
                    steer.y = BoidSettings.BoundaryWeight;
                }
                else if (currentPosition.y > maxBounds.y)
                {
                    steer.y = -BoidSettings.BoundaryWeight;
                }

                if (currentPosition.z < minBounds.z)
                {
                    steer.z = BoidSettings.BoundaryWeight;
                }
                else if (currentPosition.z > maxBounds.z)
                {
                    steer.z = -BoidSettings.BoundaryWeight;
                }

                if (!math.all(steer == float3.zero))
                {
                    // Apply steering to bring boid back inside the boundary
                    currentVelocity += math.normalize(steer) * BoidSettings.BoundaryWeight * DeltaTime;
                }

                // Update components
                Entity entity = Entities[index];

                var transform = LocalTransformLookup[entity];
                transform.Position = currentPosition;
                transform.Rotation = quaternion.LookRotationSafe(currentVelocity, math.up());
                LocalTransformLookup[entity] = transform;

                var velocity = VelocityLookup[entity];
                velocity.Velocity = currentVelocity;
                VelocityLookup[entity] = velocity;
            }
        }
    }
}
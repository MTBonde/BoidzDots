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
        public float Speed;
        public float3 Direction;
    }

    /// <summary>
    /// A crude test implementation of a boid behavior system.
    /// No optimizations have been made, this is just a simple test.
    /// First retrieve the boid settings from the singleton, then query for all boids in the scene, with specefic components.
    /// Allocate memory for nativearrays to store boid data and entities.
    /// then create and schuule data collections and behavior jobs.
    /// Last dispose of native arrays
    /// </summary>
    [BurstCompile]
    public partial struct BoidBehaviorSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { state.RequireForUpdate<BoidSettings>(); }

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

            // Get boid count
            int boidCount = boidQuery.CalculateEntityCount();

            NativeArray<BoidData> boidDataArray = new NativeArray<BoidData>(boidCount, Allocator.TempJob);
            NativeArray<Entity> entities = boidQuery.ToEntityArray(Allocator.TempJob);

            // Creat and schedule data collection job
            var collectJob = new CollectBoidDataJob
            {
                BoidDataArray = boidDataArray
            };
            state.Dependency = collectJob.ScheduleParallel(boidQuery, state.Dependency);
            state.Dependency.Complete();

            // Create and schedule boid behavior job
            var boidBehaviorJob = new BoidBehaviorJob
            {
                DeltaTime = deltaTime,
                BoidSettings = boidSettings,
                BoidDataArray = boidDataArray,
                Entities = entities,
                LocalTransformLookup = state.GetComponentLookup<LocalTransform>(false),
                DirectionLookup = state.GetComponentLookup<DirectionComponent>(false),
                SpeedLookup = state.GetComponentLookup<MoveSpeedComponent>(false)
            };
            state.Dependency = boidBehaviorJob.Schedule(boidCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Dispose of native arrays
            boidDataArray.Dispose();
            entities.Dispose();
        }

        /// <summary>
        /// A job that collects boid data such as position and velocity for all boids in the scene.
        /// This job processes entities with the LocalTransform and VelocityComponent components,
        /// storing their data into a NativeArray of BoidData.
        /// </summary>
        [BurstCompile]
        public partial struct CollectBoidDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<BoidData> BoidDataArray;

            public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in DirectionComponent direction, in MoveSpeedComponent speed)
            {
                BoidDataArray[index] = new BoidData
                {
                    Position = transform.Position,
                    Speed = speed.Speed,
                    Direction = math.normalize(direction.Direction)
                };
            }
        }

        /// <summary>
        /// A job for handling individual boid behavior updates in parallel.
        /// It calculates the movement for each boid based on its velocity and the provided settings.
        /// The job works in parallel for each boid in the BoidDataArray.
        /// </summary>
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
            public ComponentLookup<DirectionComponent> DirectionLookup;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<MoveSpeedComponent> SpeedLookup;
            
            public void Execute(int index)
            {
                BoidData currentBoid = BoidDataArray[index];
                float3 currentPosition = currentBoid.Position;
                float currentSpeed = currentBoid.Speed;
                float3 currentDirection = currentBoid.Direction;

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;
                int neighborCount = 0;

                // Loop over all boids to find neighbors
                for (int i = 0; i < BoidDataArray.Length; i++)
                {
                    if (i == index) continue;

                    float3 neighborPosition = BoidDataArray[i].Position;
                    float3 neighborDirection = BoidDataArray[i].Direction;

                    float distance = math.distance(currentPosition, neighborPosition);

                    if (distance > 0 && distance < BoidSettings.NeighborRadius)
                    {
                        alignment += neighborDirection;
                        cohesion += neighborPosition;
                        separation += (currentPosition - neighborPosition) / distance;
                        neighborCount++;
                    }
                }

                // Calculate average alignment, cohesion, and separation
                if (neighborCount > 0)
                {
                    alignment = math.normalize(alignment / neighborCount) * BoidSettings.AlignmentWeight;

                    cohesion = ((cohesion / neighborCount) - currentPosition);
                    cohesion = math.normalize(cohesion) * BoidSettings.CohesionWeight;

                    separation = math.normalize(separation / neighborCount) * BoidSettings.SeparationWeight;
                }

                // Calculate acceleration
                float3 acceleration = alignment + cohesion + separation;

                // Update direction
                currentDirection += acceleration * DeltaTime;
                currentDirection = math.normalize(currentDirection); // Ensure it stays normalized

                // Update speed
                currentSpeed = math.clamp(currentSpeed + math.length(acceleration) * DeltaTime, 0, BoidSettings.MoveSpeed);

                // Update position
                currentPosition += currentDirection * currentSpeed * DeltaTime;

                // Spherical boundary checking
                float distanceFromCenter = math.distance(currentPosition, BoidSettings.BoundaryCenter);
                if (distanceFromCenter > BoidSettings.BoundarySize)
                {
                    // Steer back toward the center
                    float3 directionToCenter = math.normalize(BoidSettings.BoundaryCenter - currentPosition);
                    float3 steer = directionToCenter * BoidSettings.BoundaryWeight;

                    // Adjust direction to incorporate steering
                    currentDirection += steer * DeltaTime;
                    currentDirection = math.normalize(currentDirection);
                }

                // Update entity components
                Entity entity = Entities[index];

                var transform = LocalTransformLookup[entity];
                transform.Position = currentPosition;
                transform.Rotation = quaternion.LookRotationSafe(currentDirection, math.up());
                LocalTransformLookup[entity] = transform;

                var speedComponent = SpeedLookup[entity];
                speedComponent.Speed = currentSpeed;
                SpeedLookup[entity] = speedComponent;

                var directionComponent = DirectionLookup[entity];
                directionComponent.Direction = currentDirection;
                DirectionLookup[entity] = directionComponent;
            }
        }
    }
}
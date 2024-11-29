using ECS.Authoring;
using ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace ECS.Systems
{
    public struct BoidData
    {
        public float3 Position;
        public float3 Velocity;
        public float Speed;
        public float3 Direction;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct BoidBehaviorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Retrieve BoidSettings singleton
            BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();

            // Get boid query
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<
                    BoidTag, 
                    LocalTransform, 
                    PhysicsVelocity,
                    DirectionComponent, 
                    MoveSpeedComponent>() 
                .Build();

            // Get boid count
            int boidCount = boidQuery.CalculateEntityCount();

            NativeArray<BoidData> boidDataArray = new NativeArray<BoidData>(boidCount, Allocator.TempJob);
            NativeArray<Entity> entities = boidQuery.ToEntityArray(Allocator.TempJob);

            // Step 1: Collect Boid Data into a NativeArray
            var collectJob = new CollectBoidDataJob
            {
                BoidDataArray = boidDataArray
            };
            state.Dependency = collectJob.ScheduleParallel(boidQuery, state.Dependency);
            state.Dependency.Complete();

            // Step 2: Build Spatial Hash Map and place boids into cells
            float cellSize = boidSettings.SpatialCellSize;
            NativeParallelMultiHashMap<int, int> spatialHashMap = new NativeParallelMultiHashMap<int, int>(boidCount, Allocator.TempJob);

            var buildHashMapJob = new BuildSpatialHashMapJob
            {
                BoidDataArray = boidDataArray,
                SpatialHashMap = spatialHashMap.AsParallelWriter(),
                CellSize = cellSize
            };
            state.Dependency = buildHashMapJob.Schedule(boidCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Step 3: Find Neighbors in the shared hashmap
            NativeArray<NeighborData> neighborDataArray = new NativeArray<NeighborData>(boidCount, Allocator.TempJob);
            var findNeighborsJob = new FindNeighborsJob
            {
                BoidDataArray = boidDataArray,
                SpatialHashMap = spatialHashMap,
                NeighborDataArray = neighborDataArray,
                CellSize = cellSize,
                BoidSettings = boidSettings
            };
            state.Dependency = findNeighborsJob.Schedule(boidCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Step 4: Calculate Boid Behavior
            var calculateBehaviorJob = new CalculateBoidBehaviorJob
            {
                BoidDataArray = boidDataArray,
                NeighborDataArray = neighborDataArray,
                DeltaTime = deltaTime,
                BoidSettings = boidSettings
            };
            state.Dependency = calculateBehaviorJob.Schedule(boidCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Step 6: Update Boids with new data
            var updateBoidsJob = new UpdateBoidsJob
            {
                BoidDataArray = boidDataArray,
                Entities = entities,
                // DeltaTime = deltaTime,
                // BoidSettings = boidSettings,
                SpeedLookup = state.GetComponentLookup<MoveSpeedComponent>(false),
                PhysicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(false),
                DirectionLookup = state.GetComponentLookup<DirectionComponent>(false)
            };
            state.Dependency = updateBoidsJob.Schedule(boidCount, 64, state.Dependency);
            state.Dependency.Complete();

            // Dispose of native arrays
            boidDataArray.Dispose();
            entities.Dispose();
            spatialHashMap.Dispose();
            neighborDataArray.Dispose();
        }

        [BurstCompile]
        public partial struct CollectBoidDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<BoidData> BoidDataArray;

            public void Execute(
                [EntityIndexInQuery] int index,
                in LocalTransform transform,
                in PhysicsVelocity velocity,
                in DirectionComponent direction,
                in MoveSpeedComponent speed)
            {
                BoidDataArray[index] = new BoidData
                {
                    Position = transform.Position,
                    Velocity = velocity.Linear,
                    Speed = speed.Speed,
                    Direction = math.normalize(direction.Direction)
                };
            }
        }

        [BurstCompile]
        public struct BuildSpatialHashMapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<BoidData> BoidDataArray;
            public NativeParallelMultiHashMap<int, int>.ParallelWriter SpatialHashMap;
            public float CellSize;

            public void Execute(int index)
            {
                BoidData boid = BoidDataArray[index];
                int3 cell = GridPosition(boid.Position, CellSize);
                int hash = Hash(cell);

                SpatialHashMap.Add(hash, index);
            }

            public static int3 GridPosition(float3 position, float cellSize) => 
                new(math.floor(position / cellSize));

            public static int Hash(int3 gridPos) =>
                (gridPos.x * 73856093) ^ (gridPos.y * 19349669) ^ (gridPos.z * 83492791);
        }

        public struct NeighborData
        {
            public float3 Alignment;
            public float3 Cohesion;
            public float3 Separation;
            public int NeighborCount;
        }

        [BurstCompile]
        public struct FindNeighborsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<BoidData> BoidDataArray;
            [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialHashMap;
            public NativeArray<NeighborData> NeighborDataArray;
            public float CellSize;
            public BoidSettings BoidSettings;

            public void Execute(int index)
            {
                BoidData currentBoid = BoidDataArray[index];
                float3 currentPosition = currentBoid.Position;

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;
                int neighborCount = 0;

                // Get neighboring cells
                int3 currentCell = BuildSpatialHashMapJob.GridPosition(currentPosition, CellSize);

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            int3 neighborCell = currentCell + new int3(x, y, z);
                            int hash = BuildSpatialHashMapJob.Hash(neighborCell);

                            NativeParallelMultiHashMapIterator<int> iterator;
                            int neighborIndex;
                            if (SpatialHashMap.TryGetFirstValue(hash, out neighborIndex, out iterator))
                            {
                                do
                                {
                                    if (neighborIndex == index)
                                        continue;

                                    BoidData neighborBoid = BoidDataArray[neighborIndex];
                                    float3 neighborPosition = neighborBoid.Position;
                                    float3 neighborDirection = neighborBoid.Direction;

                                    float distanceSq = math.lengthsq(currentPosition - neighborPosition);
                                    float neighborRadiusSq = BoidSettings.NeighborRadius * BoidSettings.NeighborRadius;

                                    if (distanceSq < neighborRadiusSq)
                                    {
                                        alignment += neighborDirection;
                                        cohesion += neighborPosition;
                                        separation += (currentPosition - neighborPosition);
                                        neighborCount++;
                                        
                                        if (neighborCount >= BoidSettings.MaxNeighbors)
                                            break;
                                    }
                                } while (SpatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
                            }
                        }
                    }
                }

                NeighborDataArray[index] = new NeighborData
                {
                    Alignment = alignment,
                    Cohesion = cohesion,
                    Separation = separation,
                    NeighborCount = neighborCount
                };
            }
        }

        [BurstCompile]
        public struct CalculateBoidBehaviorJob : IJobParallelFor
        {
            public NativeArray<BoidData> BoidDataArray;
            [ReadOnly] public NativeArray<NeighborData> NeighborDataArray;
            public float DeltaTime;
            public BoidSettings BoidSettings;

            public void Execute(int index)
            {
                BoidData boid = BoidDataArray[index];
                NeighborData neighborData = NeighborDataArray[index];

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;

                if (neighborData.NeighborCount > 0)
                {
                    alignment = math.normalize(neighborData.Alignment / neighborData.NeighborCount) * BoidSettings.AlignmentWeight;

                    cohesion = ((neighborData.Cohesion / neighborData.NeighborCount) - boid.Position);
                    cohesion = math.normalize(cohesion) * BoidSettings.CohesionWeight;

                    separation = math.normalize(neighborData.Separation / neighborData.NeighborCount) * BoidSettings.SeparationWeight;
                }

                // Calculate acceleration
                float3 acceleration = alignment + cohesion + separation;

                // Update direction
                boid.Direction += acceleration * DeltaTime;
                boid.Direction = math.normalize(boid.Direction);

                // Update speed
                boid.Speed = math.clamp(boid.Speed + math.length(acceleration) * DeltaTime, 0, BoidSettings.MoveSpeed);
               
                // Spherical boundary checking
                float distanceFromCenter = math.distance(boid.Position, BoidSettings.BoundaryCenter);
                if (distanceFromCenter > BoidSettings.BoundarySize)
                {
                    // Steer back toward the center
                    float3 directionToCenter = math.normalize(BoidSettings.BoundaryCenter - boid.Position);
                    float3 steer = directionToCenter * BoidSettings.BoundaryWeight;
                
                    // Adjust direction to incorporate steering
                    boid.Direction += steer * DeltaTime;
                    boid.Direction = math.normalize(boid.Direction);
                }

                BoidDataArray[index] = boid;
            }
        }

        [BurstCompile]
        public struct UpdateBoidsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<BoidData> BoidDataArray;
            [ReadOnly] public NativeArray<Entity> Entities;
            
            // public float DeltaTime;
            // public BoidSettings BoidSettings;

            [NativeDisableParallelForRestriction] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<DirectionComponent> DirectionLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<MoveSpeedComponent> SpeedLookup;

            public void Execute(int index)
            {
                BoidData boid = BoidDataArray[index];
                Entity entity = Entities[index];

                var velocity = PhysicsVelocityLookup[entity];

                // Apply calculated velocity to PhysicsVelocity
                velocity.Linear = boid.Direction * boid.Speed;
                PhysicsVelocityLookup[entity] = velocity;
                
                // Update DirectionComponent
                DirectionComponent directionComponent = new DirectionComponent { Direction = boid.Direction };
                DirectionLookup[entity] = directionComponent;
                
                // Update MoveSpeedComponent
                var speedComponent = SpeedLookup[entity];
                speedComponent.Speed = boid.Speed;
                SpeedLookup[entity] = speedComponent;
            }
        }
    }
}
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
                .WithAll<BoidTag, LocalTransform, DirectionComponent, MoveSpeedComponent>()
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
            JobHandle collectHandle = collectJob.ScheduleParallel(boidQuery, state.Dependency);
            //state.Dependency = collectJob.ScheduleParallel(boidQuery, state.Dependency);
            //state.Dependency.Complete();

            // Step 2: Build Spatial Hash Map and place boids into cells
            float cellSize = boidSettings.SpatialCellSize;
            NativeParallelMultiHashMap<int, int> spatialHashMap = new NativeParallelMultiHashMap<int, int>(boidCount, Allocator.TempJob);

            var buildHashMapJob = new BuildSpatialHashMapJob
            {
                BoidDataArray = boidDataArray,
                SpatialHashMap = spatialHashMap.AsParallelWriter(),
                CellSize = cellSize
            };
            JobHandle buildHashMapHandle = buildHashMapJob.Schedule(boidCount, 64, collectHandle);
            //state.Dependency = buildHashMapJob.Schedule(boidCount, 64, state.Dependency);
            //state.Dependency.Complete();

            // Step 3: Find Neighbors in the shared hashmap
            int batchSize = 64;
            NativeArray<NeighborData> neighborDataArray = new NativeArray<NeighborData>(boidCount, Allocator.TempJob);
            var findNeighborsJob = new FindNeighborsJob
            {
                BoidDataArray = boidDataArray,
                SpatialHashMap = spatialHashMap,
                NeighborDataArray = neighborDataArray,
                CellSize = cellSize,
                BoidSettings = boidSettings
            };
            JobHandle findNeighborsHandle = findNeighborsJob.Schedule(boidCount, batchSize, buildHashMapHandle);

            //state.Dependency = findNeighborsJob.Schedule(boidCount, batchSize, state.Dependency);
            //state.Dependency.Complete();

            // Step 4: Calculate Boid Behavior
            var calculateBehaviorJob = new CalculateBoidBehaviorJob
            {
                BoidDataArray = boidDataArray,
                NeighborDataArray = neighborDataArray,
                DeltaTime = deltaTime,
                BoidSettings = boidSettings
            };
            JobHandle calculateBehaviorHandle = calculateBehaviorJob.Schedule(boidCount, batchSize, findNeighborsHandle);
            //state.Dependency = calculateBehaviorJob.Schedule(boidCount, batchSize, state.Dependency);
            //state.Dependency.Complete();

            // Step 5: Update Boids with new data
            var updateBoidsJob = new UpdateBoidsJob
            {
                BoidDataArray = boidDataArray
            };
            JobHandle updateBoidsHandle = updateBoidsJob.ScheduleParallel(boidQuery, calculateBehaviorHandle);

            // Step 6: Ensure all jobs are completed before disposing resources
            state.Dependency = updateBoidsHandle;

            // Dispose of native arrays after all jobs are completed
            boidDataArray.Dispose(updateBoidsHandle);
            entities.Dispose(updateBoidsHandle);
            spatialHashMap.Dispose(updateBoidsHandle);
            neighborDataArray.Dispose(updateBoidsHandle);
        }

        [BurstCompile]
        public partial struct CollectBoidDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<BoidData> BoidDataArray;

            public void Execute(
                [EntityIndexInQuery] int index,
                in LocalTransform transform,
                in DirectionComponent direction,
                in MoveSpeedComponent speed)
            {
                BoidDataArray[index] = new BoidData
                {
                    Position = transform.Position,
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
        
        // Precompute all possible neighbor offsets in a 3D grid, including the center point
        static readonly int3[] neighborOffsets = new int3[]
        {
            new int3(-1, -1, -1), new int3(-1, -1, 0), new int3(-1, -1, 1),
            new int3(-1, 0, -1), new int3(-1, 0, 0), new int3(-1, 0, 1),
            new int3(-1, 1, -1), new int3(-1, 1, 0), new int3(-1, 1, 1),
    
            new int3(0, -1, -1), new int3(0, -1, 0), new int3(0, -1, 1),
            new int3(0, 0, -1), new int3(0, 0, 0), new int3(0, 0, 1), 
            new int3(0, 1, -1), new int3(0, 1, 0), new int3(0, 1, 1),
    
            new int3(1, -1, -1), new int3(1, -1, 0), new int3(1, -1, 1),
            new int3(1, 0, -1), new int3(1, 0, 0), new int3(1, 0, 1),
            new int3(1, 1, -1), new int3(1, 1, 0), new int3(1, 1, 1),
        };

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
                int3 currentCell = BuildSpatialHashMapJob.GridPosition(currentPosition, CellSize);

                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                float3 separation = float3.zero;
                int neighborCount = 0;

                for (int i = 0; i < neighborOffsets.Length; i++)
                {
                    int3 neighborCell = currentCell + neighborOffsets[i];
                    int hash = BuildSpatialHashMapJob.Hash(neighborCell);

                    if (SpatialHashMap.TryGetFirstValue(hash, out int neighborIndex, out var iterator))
                    {
                        do
                        {
                            // Skip self
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
        public partial struct UpdateBoidsJob : IJobEntity
        {
            [ReadOnly] public NativeArray<BoidData> BoidDataArray;

            public void Execute(
                [EntityIndexInQuery] int index,
                ref LocalTransform transform,
                ref DirectionComponent direction,
                ref MoveSpeedComponent speed)
            {
                BoidData boid = BoidDataArray[index];

                transform.Position = boid.Position;
                transform.Rotation = quaternion.LookRotationSafe(boid.Direction, math.up());

                speed.Speed = boid.Speed;
                direction.Direction = boid.Direction;
            }
        }
    }
}
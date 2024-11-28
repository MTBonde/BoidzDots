// using ECS.Authoring;
// using ECS.Components;
// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// namespace ECS.Systems
// {
//     [BurstCompile]
//     [UpdateAfter(typeof(MoveSystem))]
//     public partial struct BoidBoundaryConstraintSystem : ISystem
//     {
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<BoidSettings>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             // Retrieve BoidSettings singleton
//             BoidSettings boidSettings = SystemAPI.GetSingleton<BoidSettings>();
//
//             var boundaryJob = new BoundaryConstraintJob
//             {
//                 BoundaryCenter = boidSettings.BoundaryCenter,
//                 BoundarySize = boidSettings.BoundarySize,
//                 BoundaryWeight = boidSettings.BoundaryWeight,
//                 BoundaryType = boidSettings.BoundaryType,
//                 DeltaTime = SystemAPI.Time.DeltaTime
//             };
//
//             state.Dependency = boundaryJob.ScheduleParallel(state.Dependency);
//         }
//
//         [BurstCompile]
//         public partial struct BoundaryConstraintJob : IJobEntity
//         {
//             public float3 BoundaryCenter;
//             public float BoundarySize;
//             public float BoundaryWeight;
//             public BoundaryType BoundaryType;
//             public float DeltaTime;
//
//             public void Execute(ref LocalTransform transform, ref DirectionComponent direction)
//             {
//                 float3 position = transform.Position;
//
//                 // Handle boundary logic based on type
//                 switch (BoundaryType)
//                 {
//                     case BoundaryType.Sphere:
//                         ApplySphericalBoundary(ref position, ref direction);
//                         break;
//
//                     case BoundaryType.Box:
//                         ApplyBoxBoundary(ref position, ref direction);
//                         break;
//
//                     case BoundaryType.Donut:
//                         ApplyToroidalBoundary(ref position);
//                         break;
//                 }
//
//                 transform.Position = position;
//             }
//
//             private void ApplySphericalBoundary(ref float3 position, ref DirectionComponent direction)
//             {
//                 float distanceFromCenter = math.distance(position, BoundaryCenter);
//                 if (distanceFromCenter > BoundarySize)
//                 {
//                     // Steer gently back toward the center
//                     float3 directionToCenter = math.normalize(BoundaryCenter - position);
//                     float3 steer = directionToCenter * BoundaryWeight;
//
//                     // Gradually adjust direction
//                     direction.Direction += steer * DeltaTime;
//                     direction.Direction = math.normalize(direction.Direction);
//                 }
//             }
//
//             private void ApplyBoxBoundary(ref float3 position, ref DirectionComponent direction)
//             {
//                 float3 steering = float3.zero;
//
//                 // Calculate steering for each axis
//                 if (position.x < BoundaryCenter.x - BoundarySize)
//                     steering.x = 1; // Steer right
//                 else if (position.x > BoundaryCenter.x + BoundarySize)
//                     steering.x = -1; // Steer left
//
//                 if (position.y < BoundaryCenter.y - BoundarySize)
//                     steering.y = 1; // Steer up
//                 else if (position.y > BoundaryCenter.y + BoundarySize)
//                     steering.y = -1; // Steer down
//
//                 if (position.z < BoundaryCenter.z - BoundarySize)
//                     steering.z = 1; // Steer forward
//                 else if (position.z > BoundaryCenter.z + BoundarySize)
//                     steering.z = -1; // Steer backward
//
//                 // Apply steering to direction if steering is non-zero
//                 if (math.lengthsq(steering) > 0) 
//                 {
//                     direction.Direction += steering * BoundaryWeight * DeltaTime;
//                     direction.Direction = math.normalize(direction.Direction);
//                 }
//             }
//
//             private void ApplyToroidalBoundary(ref float3 position)
//             {
//                 float halfSize = BoundarySize / 2f;
//
//                 // Wrap position around boundary edges
//                 position.x = WrapAround(position.x, BoundaryCenter.x - halfSize, BoundaryCenter.x + halfSize);
//                 position.y = WrapAround(position.y, BoundaryCenter.y - halfSize, BoundaryCenter.y + halfSize);
//                 position.z = WrapAround(position.z, BoundaryCenter.z - halfSize, BoundaryCenter.z + halfSize);
//             }
//
//             private static float WrapAround(float value, float min, float max)
//             {
//                 if (value < min)
//                     return max - (min - value);
//                 if (value > max)
//                     return min + (value - max);
//                 return value;
//             }
//         }
//     }
// }

using ECS.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    /// <summary>
    /// Represents a movement system in the Unity ECS framework, handling entity movement logic. 
    /// Implements the ISystem interface, which enforces the use of unmanaged data types (data not
    /// managed by the garbage collector) for optimal memory and performance.
    /// Unmanaged types enable rapid data processing with Unity's job system and burst compiler.
    /// </summary>
    [UpdateAfter(typeof(BoidBehaviorSystem))]
    public partial struct MoveSystem : ISystem
    {
        /// <summary>
        /// Initializes the system with a requirement for MoveSpeedComponent and DirectionComponent, 
        /// ensuring that OnUpdate only runs when entities with these components exist. This approach 
        /// optimizes performance by avoiding unnecessary updates.
        /// </summary>
        /// <param name="state">The current state of the system.</param>
        public void OnCreate(ref SystemState state)
        {
            // Require entities to have both MoveSpeedComponent and DirectionComponent
            // Direction is marked as readwrite to allow modification during execution
            state.RequireForUpdate(state.GetEntityQuery(
                ComponentType.ReadOnly<MoveSpeedComponent>(),
                ComponentType.ReadWrite<DirectionComponent>()));
        }

        /// <summary>
        /// Create a move job and schedule it to run in parallel.
        /// </summary>
        /// <param name="state"></param>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create a new MoveJob, pass DeltaTime, and schedule it with dependency handling
            MoveJob job = new MoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            
            // Schedule the job to run in parallel with dependency tracking
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Represents a job for moving entities, using Unity's ECS framework.
        /// Implements the IJobEntity interface to execute movement logic in parallel for each entity.
        /// The job adjusts the local transform of each entity based on its speed and direction components,
        /// taking into account the elapsed time between frames.
        /// </summary>
        [BurstCompile]
        public partial struct MoveJob : IJobEntity
        {
            public float DeltaTime;
        
            /// <summary>
            /// Updates the entity’s position and orientation based on its speed and direction, applied at each
            /// frame's DeltaTime.
            /// 'in' parameters are read-only, 'ref' parameters are read/write, and 'out' would be write-only.
            /// A struct is a value type and passed by value, so 'ref' is used to pass by reference, so we can modify data.
            /// </summary>
            /// <param name="moveSpeed">Component containing the speed of the entity.</param>
            /// <param name="direction">Component containing the direction information of the entity. This will be modified during execution.</param>
            /// <param name="transform">Component containing the local transform of the entity. This will be modified during execution.</param>
            public void Execute(
                ref LocalTransform transform,
                in DirectionComponent direction,
                in MoveSpeedComponent moveSpeed)
            {
                // Update position based on direction and speed
                transform = transform.Translate(direction.Direction * moveSpeed.Speed * DeltaTime);

                // Update rotation to face the movement direction
                transform.Rotation = quaternion.LookRotationSafe(direction.Direction, math.up());
            }
        }
    }
}

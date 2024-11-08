using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    /// <summary>
    /// NOT YET IMPLEMENTED
    /// </summary>
    partial struct BoidMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Require entities to have VelocityComponent
            state.RequireForUpdate(state.GetEntityQuery(
                ComponentType.ReadWrite<VelocityComponent>(),
                ComponentType.ReadOnly<BoidTag>(),
                ComponentType.ReadOnly<BoidBehaviourComponent>()));
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }
    }
}

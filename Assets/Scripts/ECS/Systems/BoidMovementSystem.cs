using ECS.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECS.Systems
{
    partial struct BoidMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(
                ComponentType.ReadOnly<MoveSpeedComponent>(),
                ComponentType.ReadWrite<DirectionComponent>()));
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

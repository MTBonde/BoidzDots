using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct DirectionComponent : IComponentData
    {
        public float3 Direction;
    }
}
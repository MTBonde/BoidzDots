using Unity.Entities;
using Unity.Mathematics;

namespace ECS.Components
{
    public struct VelocityComponent : IComponentData
    {
        public float3 Velocity;
    }
}
// Authoring class for setting up boid entity properties in the Unity Inspector.

using ECS.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// Represents the authoring class for Boids in the Unity Inspector,
    /// Authoring allows me to set the properties such as speed and direction in the Unity Inspector.
    /// The BoidAuthoringBaker class then converts these properties into ECS-compatible component data.
    /// </summary>
    public class BoidPrefabAuthoring : MonoBehaviour
    {
        // Speed of the boid, settable in the Unity Inspector.
        public float Speed;

        // Direction of the boid as a float3, settable in the Unity Inspector.
        public float3 Direction;

        /// <summary>
        /// Inner baking class responsible for baking authoring data into ECS-compatible components.
        /// </summary>
        private class BoidPrefabAuthoringBaker : Baker<BoidPrefabAuthoring>
        {
            // Overrides Bake method to add components to the boid entity.
            public override void Bake(BoidPrefabAuthoring prefabAuthoring)
            {
                // Retrieves the entity and configures it for dynamic transformation, enabling runtime movement.
                // 'Dynamic' here allows the entity's position and rotation to be frequently updated, ideal for
                // entities requiring constant transformation, such as moving boids. Unity ECS will treat this 
                // entity as one that will undergo regular updates, allocating resources accordingly.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Adds BoidBehaviourComponent to the entity with default values.
                AddComponent(entity, new BoidBehaviourComponent {});
                AddComponent(entity, new BoidTag {});
                
                // Adds MoveSpeedComponent to the entity with the speed value from BoidAuthoring.
                AddComponent(entity, new MoveSpeedComponent { Speed = prefabAuthoring.Speed });
                
                // Adds DirectionComponent to the entity with the direction value from BoidAuthoring.
                AddComponent(entity, new DirectionComponent { Direction = prefabAuthoring.Direction });
            }
        }
    }
}
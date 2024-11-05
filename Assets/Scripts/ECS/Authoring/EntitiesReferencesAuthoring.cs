using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// This class is a container for storing references to entities in the Unity Inspector.
    /// in here we can add prefabs to be used in the ECS world.
    /// </summary>
    public class EntitiesReferencesAuthoring : MonoBehaviour
    {
        // Set the boid prefab in the Inspector.
        public GameObject BoidPrefabGameObject;

        /// <summary>
        /// This class is responsible for converting the EntitiesReferencesAuthoring MonoBehaviour
        /// into an ECS compatible format during the conversion process.
        /// </summary>
        public class Baker : Baker<EntitiesReferencesAuthoring>
        {
            // Overrides Bake method to add components to the boid entity.
            public override void Bake(EntitiesReferencesAuthoring authoring)
            {
                // Retrieves the entity and configures it for dynamic transformation, enabling runtime movement.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Adds EntitiesReferences component to the entity with the BoidPrefabEntity value from EntitiesReferencesAuthoring.
                AddComponent(entity, new EntitiesReferences
                {
                    BoidPrefabEntity = GetEntity(authoring.BoidPrefabGameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    
    /// <summary>
    /// The component data for storing references to entities in the ECS world.
    /// As the this component is used from this class we can just place it here instead of a seperate class.
    /// </summary>
    public struct EntitiesReferences : IComponentData
    {
        public Entity BoidPrefabEntity;
    }
}

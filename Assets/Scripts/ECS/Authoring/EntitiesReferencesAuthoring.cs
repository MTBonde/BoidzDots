using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
    /// <summary>
    /// The EntitiesReferencesAuthoring class is used to reference a prefab GameObject in Unity's ECS system.
    /// </summary>
    /// <remarks>
    /// This class provides a way to link a GameObject prefab to an Entity prefab in Unity's ECS system.
    /// </remarks>
    public class EntitiesReferencesAuthoring : MonoBehaviour
    {
        public GameObject BoidPrefabGameObject;


        public class Baker : Baker<EntitiesReferencesAuthoring>
        {
            public override void Bake(EntitiesReferencesAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EntitiesReferences
                {
                    BoidPrefabEntity = GetEntity(authoring.BoidPrefabGameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
    
    public struct EntitiesReferences : IComponentData
    {
        public Entity BoidPrefabEntity;
    }
}

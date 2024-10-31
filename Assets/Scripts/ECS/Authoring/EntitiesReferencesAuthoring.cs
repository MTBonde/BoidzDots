using Unity.Entities;
using UnityEngine;

namespace ECS.Authoring
{
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

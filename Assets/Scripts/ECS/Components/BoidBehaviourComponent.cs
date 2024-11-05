using Unity.Entities;

namespace ECS.Components
{
    public struct BoidBehaviourComponent : IComponentData
    {
        public float AlignmentWeight;
        public float CohesionWeight;
        public float SeparationWeight;
        
        // saved for later
        // public float NeighborRadius;
        // public float AvoidanceRadius;
        // public float PredatorAvoidanceWeight;
        // public float TargetSeekingWeight;
        // public float WanderWeight;
        //
        // public float MaxMoveSpeed;
        // public float RotationSpeed;
        // public float MaxSteerForce;
    }
}

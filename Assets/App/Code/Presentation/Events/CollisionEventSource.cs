using UnityEngine;

namespace App.Code.Presentation.Events
{
    public sealed class CollisionEventSource : ScenarioEventSource
    {
        [SerializeField] private ActorFilter actor = new ActorFilter();
        [SerializeField, Min(0f)] private float minimumImpactSpeed;

        public void Configure(ActorFilter filter) => actor = filter;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < minimumImpactSpeed) return;

            var source = collision.rigidbody != null ? collision.rigidbody.gameObject : collision.gameObject;
            if (actor.Matches(source)) Raise(source);
        }
    }
}

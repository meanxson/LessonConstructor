using UnityEngine;

namespace App.Code.Presentation.Events
{
    public sealed class TriggerEventSource : ScenarioEventSource
    {
        [SerializeField] private ActorFilter actor = new ActorFilter();

        public void Configure(ActorFilter filter) => actor = filter;

        private void OnTriggerEnter(Collider other)
        {
            var source = ActorFilter.ActorOf(other);
            if (actor.Matches(source)) Raise(source);
        }
    }
}

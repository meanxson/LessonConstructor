using System;
using UnityEngine;

namespace App.Code.Presentation.Events
{
    public readonly struct ScenarioSignal
    {
        public ScenarioSignal(ScenarioEventSource source, GameObject actor)
        {
            Source = source;
            Actor = actor;
        }

        public ScenarioEventSource Source { get; }

        public GameObject Actor { get; }
    }

    public abstract class ScenarioEventSource : MonoBehaviour
    {
        [SerializeField] private string label;

        public event Action<ScenarioSignal> Raised;

        public string Label => string.IsNullOrWhiteSpace(label) ? gameObject.name : label;

        protected void Raise(GameObject actor)
        {
            if (!isActiveAndEnabled) return;
            Raised?.Invoke(new ScenarioSignal(this, actor));
        }
    }

    [Serializable]
    public sealed class ActorFilter
    {
        [SerializeField] private GameObject requiredObject;
        [SerializeField] private string requiredTag = string.Empty;

        public ActorFilter()
        {
        }

        public ActorFilter(GameObject requiredObject, string requiredTag)
        {
            this.requiredObject = requiredObject;
            this.requiredTag = requiredTag ?? string.Empty;
        }

        public bool Matches(GameObject actor)
        {
            if (actor == null) return false;
            if (requiredObject != null && actor != requiredObject && !actor.transform.IsChildOf(requiredObject.transform)) return false;
            return string.IsNullOrEmpty(requiredTag) || actor.CompareTag(requiredTag);
        }

        public static GameObject ActorOf(Collider collider) =>
            collider.attachedRigidbody != null ? collider.attachedRigidbody.gameObject : collider.gameObject;
    }
}

using UnityEngine;

namespace App.Code.Presentation.Events
{
    public sealed class UnityEventSource : ScenarioEventSource
    {
        public void Emit() => Raise(gameObject);

        public void EmitFrom(GameObject actor) => Raise(actor != null ? actor : gameObject);
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace App.Code.Presentation.Events
{
    public sealed class ButtonEventSource : ScenarioEventSource
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick() => Raise(gameObject);
    }
}

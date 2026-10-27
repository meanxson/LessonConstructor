using UnityEngine;
using UnityEngine.UI;

namespace App.Code.Presentation.State
{
    public sealed class ToggleStateView : MonoBehaviour
    {
        [SerializeField] private StateFlag flag;

        private Toggle _toggle;

        private void Start()
        {
            _toggle = GetComponent<Toggle>();
            if (flag == null) return;

            _toggle.SetIsOnWithoutNotify(flag.Value);
            flag.Changed += OnFlagChanged;
        }

        private void OnDestroy()
        {
            if (flag != null) flag.Changed -= OnFlagChanged;
        }

        private void OnFlagChanged(bool value) => _toggle.SetIsOnWithoutNotify(value);
    }
}

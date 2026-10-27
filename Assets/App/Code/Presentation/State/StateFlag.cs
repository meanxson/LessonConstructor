using System;
using UnityEngine;
using UnityEngine.Events;

namespace App.Code.Presentation.State
{
    public sealed class StateFlag : MonoBehaviour, IScenarioResettable
    {
        [SerializeField] private string label;
        [SerializeField] private bool initialValue;
        [SerializeField] private UnityEvent onTurnedOn = new UnityEvent();
        [SerializeField] private UnityEvent onTurnedOff = new UnityEvent();

        private bool _initialized;
        private bool _value;

        public event Action<bool> Changed;

        public string Label => string.IsNullOrWhiteSpace(label) ? gameObject.name : label;

        public bool Value
        {
            get
            {
                EnsureInitialized();
                return _value;
            }
        }

        public UnityEvent OnTurnedOn => onTurnedOn;

        public UnityEvent OnTurnedOff => onTurnedOff;

        public void Set(bool value)
        {
            EnsureInitialized();
            if (_value == value) return;

            _value = value;
            Changed?.Invoke(value);
            (value ? onTurnedOn : onTurnedOff).Invoke();
        }

        public void Toggle() => Set(!Value);

        public void ResetState()
        {
            _initialized = true;
            _value = initialValue;
            Changed?.Invoke(_value);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _value = initialValue;
        }
    }
}

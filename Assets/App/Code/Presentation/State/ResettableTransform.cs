using UnityEngine;

namespace App.Code.Presentation.State
{
    public sealed class ResettableTransform : MonoBehaviour, IScenarioResettable
    {
        private Vector3 _position;
        private Quaternion _rotation;
        private Rigidbody _body;

        private void Awake()
        {
            _position = transform.position;
            _rotation = transform.rotation;
            _body = GetComponent<Rigidbody>();
        }

        public void ResetState()
        {
            if (_body != null && !_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.position = _position;
                _body.rotation = _rotation;
            }

            transform.SetPositionAndRotation(_position, _rotation);
        }
    }
}

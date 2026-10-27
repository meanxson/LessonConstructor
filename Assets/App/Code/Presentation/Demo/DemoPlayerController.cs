using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Code.Presentation.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DemoPlayerController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 5f;

        private Rigidbody _body;
        private Vector3 _input;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _input = Vector3.zero;
                return;
            }

            var x = Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed, keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
            var z = Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
            _input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
        }

        private void FixedUpdate()
        {
            var velocity = _input * speed;
            velocity.y = _body.linearVelocity.y;
            _body.linearVelocity = velocity;
        }

        private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}

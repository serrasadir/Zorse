using UnityEngine;
using UnityEngine.InputSystem;

namespace BlobSurvivor.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private VirtualJoystick _virtualJoystick;

        public Vector2 MoveDirection { get; private set; }

        private InputAction _moveAction;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("Dpad")
                .With("Up",    "<Keyboard>/w")
                .With("Up",    "<Keyboard>/upArrow")
                .With("Down",  "<Keyboard>/s")
                .With("Down",  "<Keyboard>/downArrow")
                .With("Left",  "<Keyboard>/a")
                .With("Left",  "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.Enable();
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            _moveAction?.Dispose();
        }

        private void Update()
        {
            Vector2 keyboardDir = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            if (keyboardDir.sqrMagnitude > 0.01f)
            {
               
                MoveDirection = keyboardDir.normalized;
                return;
            }

            MoveDirection = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;
        }

        public void SetJoystick(VirtualJoystick joystick) => _virtualJoystick = joystick;
    }
}

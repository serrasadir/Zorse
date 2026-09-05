using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BlobSurvivor.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        // 2026-09-05: görsel joystick kaldırıldı (Serra: "elimi kaydırdıkça oynatsın,
        // ekranda görsel olmasın"). Parmak nerede basılırsa oradan itibaren sürüklenen
        // mesafe/yön hareketi belirler — hiçbir Canvas/Image kurulumu gerekmiyor.
        [SerializeField] private float _dragRadius = 80f; // piksel — bu kadar sürüklenince yön max güce ulaşır

        public Vector2 MoveDirection { get; private set; }

        private InputAction _moveAction;
        private Vector2 _dragStartScreenPos;
        private bool _isDragging;

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
                _isDragging = false;
                return;
            }

            MoveDirection = ReadTouchDrag();
        }

        private Vector2 ReadTouchDrag()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return Vector2.zero;

            if (pointer.press.wasPressedThisFrame)
            {
                // Basış bir UI butonunun (HUD, upgrade kartı, market vb.) üzerindeyse sürüklemeyi başlatma.
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                _isDragging = !overUI;
                _dragStartScreenPos = pointer.position.ReadValue();
            }

            if (pointer.press.wasReleasedThisFrame)
                _isDragging = false;

            if (!_isDragging || !pointer.press.isPressed)
                return Vector2.zero;

            Vector2 delta = pointer.position.ReadValue() - _dragStartScreenPos;
            if (delta.sqrMagnitude < 1f) return Vector2.zero;

            return Vector2.ClampMagnitude(delta / _dragRadius, 1f);
        }
    }
}

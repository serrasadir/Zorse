using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlobSurvivor.Input
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _radius = 80f;
        [SerializeField] private CanvasGroup _canvasGroup;

        public Vector2 Direction { get; private set; }

        private Canvas _canvas;
        private bool _isDragging;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            Hide();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsLeftHalf(eventData.position)) return;

            _isDragging = true;
            MoveBackgroundTo(eventData.position);
            Show();
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, _canvas.worldCamera, out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, _radius);
            _handle.anchoredPosition = clamped;
            Direction = clamped / _radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            Direction = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
            Hide();
        }

        private void MoveBackgroundTo(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background.parent as RectTransform, screenPosition, _canvas.worldCamera, out Vector2 localPoint);
            _background.anchoredPosition = localPoint;
        }

        private bool IsLeftHalf(Vector2 screenPosition) => screenPosition.x < Screen.width * 0.5f;

        private void Show() => _canvasGroup.alpha = 1f;
        private void Hide() => _canvasGroup.alpha = 0f;
    }
}

using UnityEngine;
using UnityEngine.UI;
using BlobSurvivor.Systems.Social;

namespace BlobSurvivor.UI
{
    // Yuvarlak oyuncu fotoğrafı (Game Center). Fotoğraf girişten sonra gecikmeli yüklenir: gelene kadar karakter portresi.
    public class AvatarView : MonoBehaviour
    {
        private const float PollInterval = 0.5f;

        private RawImage _photo;
        private Image _fallback;
        private float _timer;

        // Daire maskeli kutu kurar: parent içinde pos/size, fallback = portre (Topik).
        public static AvatarView Create(RectTransform parent, Vector2 pos, float size, Sprite fallback)
        {
            var go = new GameObject("Avatar", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(size, size);

            var mask = go.AddComponent<Image>();
            mask.sprite = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            mask.color = new Color(1f, 1f, 1f, 0.15f);
            mask.raycastTarget = false;
            go.AddComponent<Mask>().showMaskGraphic = true;

            var view = go.AddComponent<AvatarView>();
            view._fallback = Child<Image>(rect, "Portrait", size * 0.86f);
            view._fallback.sprite = fallback;
            view._fallback.preserveAspect = true;
            view._photo = Child<RawImage>(rect, "Photo", size);
            view._photo.enabled = false;
            view.Refresh();
            return view;
        }

        private static T Child<T>(RectTransform parent, string name, float size) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            T graphic = go.AddComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private void OnEnable() => Refresh();

        private void Update()
        {
            if (_photo.enabled) return; // fotoğraf geldi, bir daha sorulmaz
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = PollInterval;
            Refresh();
        }

        public void Refresh()
        {
            if (_photo == null) return;
            Texture2D avatar = AchievementTracker.Instance != null ? AchievementTracker.Instance.Avatar : null;
            _photo.texture = avatar;
            _photo.enabled = avatar != null;
            _fallback.enabled = avatar == null;
        }
    }
}

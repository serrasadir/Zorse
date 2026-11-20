using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.UI
{
    // P0-d geri bildirim katmanı: sahneye dokunmadan kök Canvas'a ayrı bir alt Canvas kurar (kenar uyarısı + olay bandı).
    // Her kare değişen öğeler statik HUD'u yeniden batch'letmesin diye ayrı Canvas (hud.md §12).
    public static class HudFeedbackLayer
    {
        private const string LayerName = "HUDFeedbackLayer";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            HUDController hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null) return;
            Canvas canvas = hud.GetComponentInParent<Canvas>(true);
            if (canvas == null) return;
            Transform root = canvas.rootCanvas.transform;
            if (root.Find(LayerName) != null) return;

            var go = new GameObject(LayerName, typeof(RectTransform));
            go.layer = root.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            go.AddComponent<Canvas>(); // iç içe Canvas: sıralama kökten
            Stretch(rect);
            // HUD'un hemen üstü: kart ekranı, duraklatma, oyun sonu bunun üstünde kalır.
            Transform top = hud.transform;
            while (top.parent != null && top.parent != root) top = top.parent;
            if (top.parent == root) rect.SetSiblingIndex(top.GetSiblingIndex() + 1);

            go.AddComponent<EdgeWarning>().Init(hud.gameObject, rect, FindHealthBar(hud, root));
            go.AddComponent<EventBanner>().Init(hud.gameObject, rect);
            go.AddComponent<MissionTracker>().Init(hud.gameObject, rect);
            go.AddComponent<ComboPopup>().Init(hud.gameObject, rect);
            go.AddComponent<GateGuide>().Init(hud.gameObject, rect); // kale: kapı oku + küçük harita
        }

        // HUD'un kendi referansı (bar taşınsa da aynı nesne); yoksa sahnedeki adıyla.
        private static Slider FindHealthBar(HUDController hud, Transform root)
        {
            FieldInfo field = typeof(HUDController).GetField("_healthBar", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.GetValue(hud) is Slider bar && bar != null) return bar;
            foreach (Slider s in root.GetComponentsInChildren<Slider>(true))
                if (s.name == "HealthBar") return s;
            Debug.LogWarning("[HudFeedbackLayer] Can barı bulunamadı; düşük can işareti yok.");
            return null;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}

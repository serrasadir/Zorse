using System.IO;
using BlobSurvivor.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-02: Canvas/SafeArea altına duraklatma butonu + panelini kurar. Tekrar çalıştırmak eskisini silip
    // yeniden kurar. Temp/pause_menu.request varsa script yüklemesinde kendiliğinden çalışır.
    public static class PauseMenuBuilder
    {
        private const string RequestPath = "Temp/pause_menu.request";
        private const string ResultPath = "Temp/pause_menu.result";
        private const string RootName = "PauseMenu";

        private static readonly Color Bone = new Color(0.96f, 0.94f, 0.88f, 1f);      // #F5F0E0
        private static readonly Color Night = new Color(0.05f, 0.05f, 0.17f, 0.85f);   // #0D0D2B
        private static readonly Color Crimson = new Color(0.545f, 0f, 0f, 1f);         // #8B0000

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                File.WriteAllText(ResultPath, EditorApplication.isPlayingOrWillChangePlaymode ? "ABORTED: Play Mode açık" : Build());
            };
        }

        [MenuItem("Tools/Blob.io/Duraklatma Menüsünü Kur")]
        private static void BuildFromMenu() => Debug.Log("[PauseMenuBuilder] " + Build());

        private static string Build()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            Transform safeArea = canvas != null ? canvas.transform.Find("SafeArea") : null;
            if (safeArea == null) return "HATA: Canvas/SafeArea bulunamadı";

            Transform existing = safeArea.Find(RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            RectTransform root = CreateRect(RootName, safeArea);
            Stretch(root);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Duraklatma Menüsü");

            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Sağ üst köşe. HUD'daki seviye yazısıyla çakışırsa Inspector'dan kaydırılabilir.
            RectTransform pauseButton = CreateButton("PauseButton", root, "II", sprite, Night, 44);
            pauseButton.anchorMin = pauseButton.anchorMax = pauseButton.pivot = new Vector2(1f, 1f);
            pauseButton.anchoredPosition = new Vector2(-30f, -30f);
            pauseButton.sizeDelta = new Vector2(100f, 100f);

            RectTransform panel = CreateRect("Panel", root);
            Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = Night; // arkadaki oyuna dokunmayı da engeller

            RectTransform title = CreateText("Title", panel, "Duraklatıldı", 72);
            title.anchorMin = title.anchorMax = new Vector2(0.5f, 0.5f);
            title.anchoredPosition = new Vector2(0f, 140f);
            title.sizeDelta = new Vector2(800f, 120f);

            RectTransform resume = CreateButton("ResumeButton", panel, "Devam", sprite, Crimson, 56);
            resume.anchorMin = resume.anchorMax = new Vector2(0.5f, 0.5f);
            resume.anchoredPosition = new Vector2(0f, -40f);
            resume.sizeDelta = new Vector2(420f, 130f);

            PauseMenu menu = root.gameObject.AddComponent<PauseMenu>();
            SerializedObject so = new SerializedObject(menu);
            so.FindProperty("_pauseButton").objectReferenceValue = pauseButton.GetComponent<Button>();
            so.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            so.FindProperty("_resumeButton").objectReferenceValue = resume.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);
            root.SetAsLastSibling(); // HUD'un üstünde çizilsin
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            return "OK: SafeArea/PauseMenu kuruldu. Sahneyi kaydet (Cmd+S).";
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateText(string name, Transform parent, string text, float size)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Bone;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateButton(string name, Transform parent, string text, Sprite sprite, Color color, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            rect.gameObject.AddComponent<Button>().targetGraphic = image;

            RectTransform label = CreateText("Label", rect, text, fontSize);
            Stretch(label);
            return rect;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Systems.Social;

namespace BlobSurvivor.UI
{
    // Profil (lobide sol üstteki ad çipi): Game Center fotoğrafı, adı ve oyuncu kimliği, rozet / rekor / en hızlı kazanma,
    // profili düzenle (ad + fotoğraf Game Center'da), arkadaş ekle, rozetlerim, sıralama. Giriş yoksa "Game Center'a gir".
    // SettingsPanel diliyle, kök Canvas'ın üstünde tam ekran; düzen ilk açılışta kodla bir kez kurulur.
    public class ProfilePanel : MonoBehaviour
    {
        private static ProfilePanel s_instance;

        private static readonly Color Dim = new Color(0.02f, 0.02f, 0.08f, 0.75f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.95f);
        private static readonly Color Chip = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Muted = new Color(0.3f, 0.3f, 0.42f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.02f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.7f);

        private Sprite _rounded;
        private AvatarView _avatar;
        private TMP_Text _name, _id, _badges, _best, _fastest, _primaryLabel, _hint;

        public static void Open(Component anyUnderCanvas, Sprite rounded, Sprite fallbackPortrait)
        {
            if (s_instance == null)
            {
                Canvas canvas = anyUnderCanvas.GetComponentInParent<Canvas>(true);
                if (canvas == null) return;
                var go = new GameObject("ProfilePanel", typeof(RectTransform));
                go.layer = canvas.gameObject.layer;
                s_instance = go.AddComponent<ProfilePanel>();
                s_instance.Build((RectTransform)canvas.rootCanvas.transform, rounded, fallbackPortrait);
            }
            s_instance.transform.SetAsLastSibling();
            s_instance.gameObject.SetActive(true);
            s_instance.Refresh();
        }

        private static AchievementTracker Tracker => AchievementTracker.Instance;

        private void OnEnable()
        {
            if (Tracker != null) Tracker.SignInChanged += Refresh;
        }

        private void OnDisable()
        {
            if (Tracker != null) Tracker.SignInChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_name == null) return;
            AchievementTracker t = Tracker;
            bool signedIn = t != null && t.IsSignedIn;
            _name.text = signedIn && !string.IsNullOrEmpty(t.PlayerName) ? t.PlayerName : "Misafir";
            _id.text = signedIn && !string.IsNullOrEmpty(t.PlayerId) ? $"Oyuncu ID: {t.PlayerId}" : "Game Center'a giriş yapılmadı";
            _badges.text = $"{(t != null ? t.UnlockedCount : 0)}/{Achievements.Count}";
            _best.text = $"{(t != null ? t.BestScore : 0):N0}";
            float win = t != null ? t.BestWinSeconds : 0f;
            _fastest.text = win > 0f ? $"{(int)(win / 60f)}:{(int)(win % 60f):00}" : "—";
            _primaryLabel.text = signedIn ? "PROFİLİ DÜZENLE" : "GAME CENTER'A GİR";
            _hint.text = signedIn ? "Ad ve fotoğraf Game Center profilinden değişir" : "Açılmazsa: iPhone Ayarlar → Game Center";
            _avatar.Refresh();
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build(RectTransform root, Sprite rounded, Sprite fallbackPortrait)
        {
            _rounded = rounded;
            var rect = (RectTransform)transform;
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Karartma: kartın dışına dokunmak kapatır. Kart kardeş (çocuğu olsa tıklama ona da çıkardı).
            RectTransform dim = Box("Dim", rect, Vector2.zero, Vector2.zero, Dim, null);
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            AddButton(dim, Close).transition = Selectable.Transition.None;

            RectTransform card = Box("Card", rect, Vector2.zero, new Vector2(900f, 1180f), CardColor, _rounded);
            card.GetComponent<Image>().raycastTarget = true;

            Text("Title", card, new Vector2(0f, 510f), new Vector2(800f, 90f), 70f, Color.white).text = "PROFİL";
            _avatar = AvatarView.Create(card, new Vector2(0f, 330f), 220f, fallbackPortrait);
            _name = Text("Name", card, new Vector2(0f, 175f), new Vector2(820f, 80f), 58f, Color.white);
            _name.overflowMode = TextOverflowModes.Ellipsis;
            _id = Text("Id", card, new Vector2(0f, 118f), new Vector2(820f, 44f), 28f, Subtle);
            _id.overflowMode = TextOverflowModes.Ellipsis;

            _badges = Stat(card, -270f, "ROZET");
            _best = Stat(card, 0f, "REKOR");
            _fastest = Stat(card, 270f, "EN HIZLI");

            RectTransform primary = Button(card, "PrimaryButton", new Vector2(0f, -150f), new Vector2(680f, 104f), Gold, () =>
            {
                if (Tracker == null) return;
                if (Tracker.IsSignedIn) Tracker.ShowProfile();
                else Tracker.SignIn();
            });
            _primaryLabel = Text("Label", primary, Vector2.zero, primary.sizeDelta, 44f, Ink);
            _hint = Text("Hint", card, new Vector2(0f, -224f), new Vector2(820f, 40f), 26f, Subtle);

            RectTransform friends = Button(card, "FriendsButton", new Vector2(0f, -300f), new Vector2(680f, 104f), Gold, () => Tracker?.ShowAddFriends());
            Text("Label", friends, Vector2.zero, friends.sizeDelta, 44f, Ink).text = "ARKADAŞ EKLE";

            RectTransform badges = Button(card, "BadgesButton", new Vector2(-172f, -422f), new Vector2(336f, 104f), Muted, () => Tracker?.ShowAchievements());
            Text("Label", badges, Vector2.zero, badges.sizeDelta, 40f, Color.white).text = "ROZETLERİM";
            RectTransform boards = Button(card, "LeaderboardButton", new Vector2(172f, -422f), new Vector2(336f, 104f), Muted, () => Tracker?.ShowLeaderboards());
            Text("Label", boards, Vector2.zero, boards.sizeDelta, 40f, Color.white).text = "SIRALAMA";

            RectTransform close = Button(card, "CloseButton", new Vector2(0f, -530f), new Vector2(360f, 84f), Chip, Close);
            Text("Label", close, Vector2.zero, close.sizeDelta, 38f, Color.white).text = "KAPAT";
        }

        private void Close() => gameObject.SetActive(false);

        private TMP_Text Stat(RectTransform card, float x, string label)
        {
            RectTransform box = Box(label + "Stat", card, new Vector2(x, 10f), new Vector2(250f, 130f), Chip, _rounded);
            Text("Label", box, new Vector2(0f, 30f), new Vector2(240f, 40f), 26f, Subtle).text = label;
            return Text("Value", box, new Vector2(0f, -18f), new Vector2(240f, 60f), 46f, Gold);
        }

        private RectTransform Button(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = Box(name, parent, pos, size, color, _rounded);
            AddButton(rect, onClick);
            return rect;
        }

        private static Button AddButton(RectTransform rect, UnityEngine.Events.UnityAction onClick)
        {
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            return button;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; }
            return rect;
        }

        private static TMP_Text Text(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}

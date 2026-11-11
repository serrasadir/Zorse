using System.Collections.Generic;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;
using BlobSurvivor.Systems.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BlobSurvivor.Core
{
    // 2026-11-04: telefonda performans ölçümü (şehir kiti v2 denemesi). Gizli başlar.
    //   Aç/kapa: ekrana 3 parmakla dokun (Editor'da F3). Durum PlayerPrefs'te kalır.
    //   Gösterir: ortalama FPS (0.5 sn), son 2 sn'nin en kötü karesi (ms, takılma), hedef FPS.
    //   "+500 kütle" düğmesi: Giant'a hızlı çıkıp en kötü anı (kamera en uzak, en çok bina ekranda) ölçmek için.
    //   "DEV" düğmesi: test paneli — coin, karakter kilitleri, skill +1/max, kütle, can, boss, kayıt sıfırlama.
    //   Panel açıkken oyun durur ve uGUI tıklamaları kapanır (alttaki karta/butona basılmasın).
    // YAYIN ÖNCESİ: Enabled = false (oyunculara hile düğmesi gitmesin). Kapalıyken hiç oluşmaz, maliyeti sıfır.
    public class PerfOverlay : MonoBehaviour
    {
        public static readonly bool Enabled = true; // const değil: sabit dal "unreachable code" uyarısı veriyordu

        private const string PrefKey = "perf_overlay";
        private const float AvgWindow = 0.5f;
        private const float WorstWindow = 2f;

        private bool _visible;
        private bool _wasThreeFingers;
        private float _accTime;
        private int _accFrames;
        private float _fps;
        private float _worstMs, _worstAccMs, _worstTimer;
        private GUIStyle _style, _button;
        private BlobGrowth _growth;

        private bool _devOpen;
        private bool _pausedByPanel;
        private bool _confirmReset;
        private Vector2 _scroll;
        private float _dragDistance;
        private const float DragClickThreshold = 12f; // piksel
        private string _message = "";
        private List<UpgradeData> _skills;
        private EventSystem _disabledEventSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Enabled) return;
            GameObject go = new GameObject("PerfOverlay");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfOverlay>();
        }

        private void Awake()
        {
            _visible = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        }

        private void Update()
        {
            HandleToggle();

            float dt = Time.unscaledDeltaTime;
            _accTime += dt;
            _accFrames++;
            if (_accTime >= AvgWindow)
            {
                _fps = _accFrames / _accTime;
                _accTime = 0f;
                _accFrames = 0;
            }
            _worstAccMs = Mathf.Max(_worstAccMs, dt * 1000f);
            _worstTimer += dt;
            if (_worstTimer >= WorstWindow)
            {
                _worstMs = _worstAccMs;
                _worstAccMs = 0f;
                _worstTimer = 0f;
            }
        }

        private void HandleToggle()
        {
            bool toggle = false;
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                int pressed = 0;
                foreach (var t in touch.touches)
                    if (t.press.isPressed) pressed++;
                bool three = pressed >= 3;
                if (three && !_wasThreeFingers) toggle = true;
                _wasThreeFingers = three;
            }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) toggle = true;
            if (!toggle) return;
            _visible = !_visible;
            PlayerPrefs.SetInt(PrefKey, _visible ? 1 : 0);
            // Gizlenince panel de kapanmalı: yoksa EventSystem kapalı kalır, duraklatma menüsüne basılamaz.
            if (!_visible && _devOpen) SetDevOpen(false);
        }

        private void OnGUI()
        {
            if (!_visible) return;
            float s = Screen.height / 900f;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _button = new GUIStyle(GUI.skin.button);
            }
            _style.fontSize = Mathf.RoundToInt(18 * s);
            _button.fontSize = Mathf.RoundToInt(16 * s);

            int target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 30; // iOS varsayılanı 30
            _style.normal.textColor = _fps >= target * 0.95f ? Color.green : _fps >= target * 0.8f ? Color.yellow : Color.red;
            Rect r = new Rect(10 * s, 60 * s, 320 * s, 90 * s);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 8 * s, r.y + 4 * s, r.width, 26 * s), $"FPS {_fps:F1} / hedef {target}", _style);
            _style.normal.textColor = _worstMs <= 1000f / target * 1.5f ? Color.white : Color.yellow;
            GUI.Label(new Rect(r.x + 8 * s, r.y + 30 * s, r.width, 26 * s), $"en kötü kare {_worstMs:F0} ms (2 sn)", _style);

            if (_growth == null) _growth = FindAnyObjectByType<BlobGrowth>();
            float y = r.yMax + 6 * s;
            if (_growth != null)
            {
                _style.normal.textColor = Color.white;
                GUI.Label(new Rect(r.x + 8 * s, r.y + 56 * s, r.width, 26 * s), $"kütle {_growth.CurrentMass:F0} · {_growth.CurrentTier}", _style);
                if (GUI.Button(new Rect(r.x, y, 150 * s, 40 * s), "+500 kütle", _button))
                    _growth.DebugAddMassWithoutXP(500f);
            }
            if (GUI.Button(new Rect(r.x + 160 * s, y, 100 * s, 40 * s), _devOpen ? "DEV ×" : "DEV", _button))
                SetDevOpen(!_devOpen);

            if (_devOpen) DrawDevPanel(s);
        }

        // ---------------------------------------------------------------- dev paneli

        private void SetDevOpen(bool open)
        {
            _devOpen = open;
            _confirmReset = false;
            _message = "";
            if (open)
            {
                _skills = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.DebugAllUpgrades() : null;
                BuildRunSummary();
                GameManager gm = GameManager.Instance;
                if (gm != null && gm.CurrentState == GameState.Playing && Time.timeScale > 0f)
                {
                    Time.timeScale = 0f;
                    _pausedByPanel = true;
                }
                EventSystem es = EventSystem.current;
                if (es != null) { es.enabled = false; _disabledEventSystem = es; }
            }
            else
            {
                if (_pausedByPanel && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                    Time.timeScale = 1f;
                _pausedByPanel = false;
                if (_disabledEventSystem != null) _disabledEventSystem.enabled = true;
                _disabledEventSystem = null;
            }
        }

        private void DrawDevPanel(float s)
        {
            float pad = 10 * s, rowH = 46 * s, gap = 6 * s;
            var panel = new Rect(pad, 210 * s, Screen.width - pad * 2, Screen.height - 230 * s);
            GUI.Box(panel, GUIContent.none);
            GUI.Box(panel, GUIContent.none); // iki kat: daha koyu

            float w = panel.width - pad * 2;
            float half = (w - gap) / 2f;
            _style.normal.textColor = new Color(1f, 0.82f, 0.25f);
            GUI.Label(new Rect(panel.x + pad, panel.y + pad, w, 30 * s), "DEV ARAÇLARI", _style);
            if (!string.IsNullOrEmpty(_message))
            {
                _style.normal.textColor = Color.white;
                GUI.Label(new Rect(panel.x + pad, panel.y + pad + 30 * s, w, 28 * s), _message, _style);
            }

            var view = new Rect(panel.x + pad, panel.y + 74 * s, w, panel.height - 84 * s);
            float contentH = ContentHeight(rowH, gap, s);
            DragScroll(view, contentH);
            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, w - 20 * s, contentH));
            float cw = w - 20 * s;
            half = (cw - gap) / 2f;
            float y = 0f;

            Header("COIN / KAYIT", ref y, cw, s);
            MetaProgression meta = MetaProgression.Instance;
            if (meta == null)
            {
                Note("MetaProgression yok.", ref y, cw, s);
            }
            else
            {
                Note($"Coin {meta.Credit:N0} · Mıknato {(meta.MiknatoUnlocked ? "açık" : "kilitli")} · biten run {meta.CompletedRuns}", ref y, cw, s);
                Note(_runSummary1, ref y, cw, s);
                Note(_runSummary2, ref y, cw, s);
                if (Btn(0, y, half, rowH, "+1.000 coin")) { meta.DevAddCredit(1000); _message = $"Coin {meta.Credit:N0}"; }
                if (Btn(half + gap, y, half, rowH, "+10.000 coin")) { meta.DevAddCredit(10000); _message = $"Coin {meta.Credit:N0}"; }
                y += rowH + gap;
                if (Btn(0, y, half, rowH, meta.MiknatoUnlocked ? "Mıknato'yu kilitle" : "Mıknato'yu aç"))
                {
                    meta.DevSetMiknatoUnlocked(!meta.MiknatoUnlocked);
                    _message = meta.MiknatoUnlocked ? "Mıknato açık" : "Mıknato kilitli";
                }
                if (Btn(half + gap, y, half, rowH, "Tüm karakterleri aç")) { meta.DevUnlockAllCharacters(); _message = "Tüm karakterler açık"; }
                y += rowH + gap;
                if (Btn(0, y, half, rowH, _confirmReset ? "Emin misin? Sıfırla" : "Kaydı sıfırla"))
                {
                    if (_confirmReset) { meta.DevReset(); _message = "Kayıt sıfırlandı"; _confirmReset = false; }
                    else _confirmReset = true;
                }
                y += rowH + gap;
            }

            GameManager gm = GameManager.Instance;
            bool inRun = gm != null && gm.CurrentState != GameState.Menu && UpgradeSystem.Instance != null;

            Header("RUN", ref y, cw, s);
            if (!inRun)
            {
                Note("Run içinde açılır (karakter seç).", ref y, cw, s);
            }
            else
            {
                if (Btn(0, y, half, rowH, "+500 kütle") && _growth != null) { _growth.DebugAddMassWithoutXP(500f); _message = $"Kütle {_growth.CurrentMass:F0}"; }
                if (Btn(half + gap, y, half, rowH, "Canı doldur"))
                {
                    BlobHealth h = _growth != null ? _growth.GetComponent<BlobHealth>() : null;
                    if (h != null) { h.Heal(h.MaxHealth); _message = "Can dolu"; }
                }
                y += rowH + gap;
                if (Btn(0, y, half, rowH, "Sıradaki boss") && BossSpawner.Instance != null) _message = BossSpawner.Instance.DebugSpawnNextStage();
                if (Btn(half + gap, y, half, rowH, "Final boss −%25"))
                {
                    FinalBossController boss = FinalBossController.Active;
                    EnemyBase enemy = boss != null ? boss.GetComponent<EnemyBase>() : null;
                    if (enemy == null) _message = "Aktif final boss yok";
                    else { enemy.TakeDamage(enemy.MaxHealth * 0.25f); _message = $"Final boss {enemy.CurrentHealth:F0}/{enemy.MaxHealth:F0}"; }
                }
                y += rowH + gap;

                Header("SKILLER", ref y, cw, s);
                if (_skills != null)
                {
                    UpgradeSystem up = UpgradeSystem.Instance;
                    float btnW = 90 * s;
                    for (int i = 0; i < _skills.Count; i++)
                    {
                        UpgradeData u = _skills[i];
                        int level = up.GetLevel(u);
                        _style.normal.textColor = level >= u.MaxLevel ? new Color(0.45f, 0.9f, 0.5f) : Color.white;
                        GUI.Label(new Rect(0, y + 8 * s, cw - btnW * 2 - gap * 2, rowH), $"{u.DisplayName}  {level}/{u.MaxLevel}", _style);
                        if (Btn(cw - btnW * 2 - gap, y, btnW, rowH, "+1")) Grant(u, 1);
                        if (Btn(cw - btnW, y, btnW, rowH, "MAX")) Grant(u, u.MaxLevel);
                        y += rowH + gap;
                    }
                }
            }

            GUI.EndScrollView();
        }

        private float ContentHeight(float rowH, float gap, float s)
        {
            int skills = _skills != null ? _skills.Count : 0;
            int rows = 2 + skills + 3;
            return 3 * 40 * s + 4 * 34 * s + rows * (rowH + gap) + 20 * s;
        }

        // Denge ölçümü: son 10 run'ın ortalaması (analytics.json, panel açılınca bir kez okunur).
        private string _runSummary1 = "", _runSummary2 = "";

        private void BuildRunSummary()
        {
            AnalyticsSaveData data = SaveSystem.Instance != null ? SaveSystem.Instance.Load() : null;
            var runs = new List<RunRecord>();
            if (data != null)
                foreach (RunRecord r in data.runs)
                    if (!string.IsNullOrEmpty(r.character)) runs.Add(r); // ölçüm alanları eklenmeden önceki kayıtlar sayılmaz
            int n = Mathf.Min(10, runs.Count);
            if (n == 0) { _runSummary1 = "Henüz ölçülen run yok (bir run bitir)."; _runSummary2 = ""; return; }

            float time = 0f; int coins = 0, banked = 0, police = 0, elite = 0, level = 0;
            int cPolice = 0, cElite = 0, cChest = 0, cBoss = 0, expired = 0;
            for (int i = runs.Count - n; i < runs.Count; i++)
            {
                RunRecord r = runs[i];
                time += r.durationSeconds; coins += r.coinsEarned; banked += r.coinsBanked;
                police += r.policeKills; elite += r.eliteKills; level += r.finalLevel;
                cPolice += r.coinsPolice; cElite += r.coinsElite; cChest += r.coinsChest; cBoss += r.coinsBoss; expired += r.coinsExpired;
            }
            int t = Mathf.RoundToInt(time / n);
            _runSummary1 = $"Son {n} run ort.: {t / 60}:{t % 60:00} · {coins / n} coin (kasaya {banked / n}) · {police / n} polis · {elite / n} elit · Sv.{level / n}";
            int total = Mathf.Max(1, cPolice + cElite + cChest + cBoss);
            _runSummary2 = $"Coin payı: polis %{100 * cPolice / total} · elit %{100 * cElite / total} · sandık %{100 * cChest / total} · boss %{100 * cBoss / total} · kaybolan {expired / n}/run";
        }

        // IMGUI kaydırma alanı dokunmatikte sürüklenmiyor: parmak sürüklemesi elle kaydırır.
        // Sürükleme sonrası parmak kalkınca altındaki düğmeye basılmış sayılmasın.
        private void DragScroll(Rect view, float contentH)
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    _dragDistance = 0f;
                    break;
                case EventType.MouseDrag when view.Contains(e.mousePosition):
                    _dragDistance += Mathf.Abs(e.delta.y);
                    _scroll.y = Mathf.Clamp(_scroll.y - e.delta.y, 0f, Mathf.Max(0f, contentH - view.height));
                    e.Use();
                    break;
                case EventType.MouseUp when _dragDistance > DragClickThreshold:
                    _dragDistance = 0f;
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        private void Grant(UpgradeData u, int times)
        {
            UpgradeSystem up = UpgradeSystem.Instance;
            if (up == null) return;
            for (int i = 0; i < times && up.GetLevel(u) < u.MaxLevel; i++) _message = up.DebugGrant(u.Id);
            _skills = up.DebugAllUpgrades(); // evrim açıldıysa listeye girsin
        }

        private bool Btn(float x, float y, float w, float h, string label) => GUI.Button(new Rect(x, y, w, h), label, _button);

        private void Header(string text, ref float y, float w, float s)
        {
            y += 6 * s;
            _style.normal.textColor = new Color(1f, 0.82f, 0.25f);
            GUI.Label(new Rect(0, y, w, 30 * s), text, _style);
            y += 34 * s;
        }

        private void Note(string text, ref float y, float w, float s)
        {
            _style.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            GUI.Label(new Rect(0, y, w, 30 * s), text, _style);
            y += 34 * s;
        }
    }
}

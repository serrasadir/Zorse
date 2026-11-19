using System.Collections.Generic;
using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Görünür silah: sopa (G_Stick_*, build_stick.py) ve Havai Fişek atarı (G_Firework_*) → Resources/FX/Weapons/G_<Model>_<form>.
    // Sopa formu skilin yeni mekaniğiyle değişir (WeaponUpgradeEffect hesaplar): 1 temel · 2 yay 180° · 3 hızlı · 4 tam tur.
    // Mesh blob görünüm mesh'iyle aynı uzayda (pivot = blob merkezi) → görünüm renderer'ının child'ı, aynı materyal;
    // gidiş yönüne dönme, geri tepme, yutkunma kendiliğinden gelir. Silah hedefe dönmez. Ateş/mermi koduna dokunmaz.
    // GameManager.ApplyCharacter her run başında Setup çağırır (eski görünümle eski silah da gider).
    public class WeaponVisual : MonoBehaviour
    {
        private const string Folder = "FX/Weapons/G_";
        private const float PopDuration = 0.35f;
        private static readonly Dictionary<string, Mesh> s_meshes = new Dictionary<string, Mesh>();

        private Transform _weapon;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private string _model;
        private int _form = -1;
        private float _popTime = -1f;
        private bool _popFromZero;
        private float _swingTime = -1f;
        private bool _swingFull;
        private float _swingArc = 120f;
        private bool _hideWhenIdle;

        // Sopa vuruşu: model blob'un ortasından gidiş yönüne uzanır (pivot = blob merkezi) ve yay boyunca
        // soldan sağa savrulur (SwingArcFx'in süpürmesiyle aynı hızda); tam tur (Sv.8) bir kez döner.
        private const float Sweep = 0.12f, Hold = 0.05f, Spin = 0.3f;

        // Sopa: sadece vururken görünür.
        public bool HideWhenIdle
        {
            set { _hideWhenIdle = value; RefreshVisible(); }
        }

        private void RefreshVisible()
        {
            if (_renderer == null || _filter == null) return;
            _renderer.enabled = _filter.sharedMesh != null && (!_hideWhenIdle || _swingTime >= 0f);
        }

        public void PlaySwing(float arc, bool fullCircle)
        {
            if (_weapon == null || _form <= 0) return;
            _swingTime = 0f;
            _swingArc = arc;
            _swingFull = fullCircle;
            UpdateSwing();
            RefreshVisible();
        }

        private void UpdateSwing()
        {
            if (_swingTime < 0f || _weapon == null) return;
            float t = _swingTime;
            _swingTime += Time.deltaTime;
            float yaw;
            if (_swingFull)
            {
                float u = Mathf.Clamp01(t / Spin);
                yaw = -90f + 360f * (1f - (1f - u) * (1f - u));
                if (t >= Spin + Hold) _swingTime = -1f;
            }
            else
            {
                float half = _swingArc * 0.5f;
                yaw = Mathf.Lerp(-half, half, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Sweep)));
                if (t >= Sweep + Hold) _swingTime = -1f;
            }
            if (_swingTime < 0f)
            {
                _weapon.localRotation = Quaternion.identity;
                RefreshVisible();
                return;
            }
            _weapon.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // childName: aynı host'a ikinci bir görünür silah (Havai Fişek atarı) takılabilsin diye.
        public void Setup(Renderer host, string model, bool visibleFromStart, string childName = "Weapon")
        {
            if (_weapon != null) Destroy(_weapon.gameObject);
            _weapon = null;
            _form = -1;
            _popTime = -1f;
            _swingTime = -1f;
            _hideWhenIdle = false;
            _model = model;
            if (host == null || string.IsNullOrEmpty(model)) return;

            var go = new GameObject(childName);
            go.layer = host.gameObject.layer;
            _weapon = go.transform;
            _weapon.SetParent(host.transform, false);
            _filter = go.AddComponent<MeshFilter>();
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = host.sharedMaterial;
            _renderer.shadowCastingMode = host.shadowCastingMode;
            _renderer.receiveShadows = host.receiveShadows;
            SetForm(visibleFromStart ? 1 : 0, pop: false);
        }

        // Havai Fişek atarı skill objesiyle birlikte silinir; mesh child'ı host renderer'da kalmasın.
        private void OnDestroy()
        {
            if (_weapon != null) Destroy(_weapon.gameObject);
        }

        // form 0 = gizli, 1.. = G_<Model>_<form> (Sopa 5 = Lamba Direği).
        public void SetForm(int form) => SetForm(form, pop: true);

        private void SetForm(int form, bool pop)
        {
            if (_weapon == null || form == _form) return;
            bool wasHidden = _form <= 0;
            _form = form;
            Mesh mesh = form > 0 ? LoadMesh(_model, form) : null;
            _filter.sharedMesh = mesh;
            RefreshVisible();
            if (mesh == null || !pop)
            {
                _popTime = -1f;
                _weapon.localScale = Vector3.one;
                return;
            }
            _popTime = 0f;           // yeni form "pop" ile gelsin (kart ekranı kapanınca oynar)
            _popFromZero = wasHidden;
            _weapon.localScale = wasHidden ? Vector3.zero : Vector3.one;
        }

        // Eksik form bir öncekine düşer (yeni form modeli gelmeden test edilebilsin).
        private static Mesh LoadMesh(string model, int form)
        {
            string path = $"{Folder}{model}_{form}";
            if (s_meshes.TryGetValue(path, out Mesh mesh)) return mesh;
            mesh = Resources.Load<Mesh>(path);
            if (mesh == null)
            {
                Debug.LogWarning($"[WeaponVisual] Resources/{path} bulunamadı.");
                if (form > 1) mesh = LoadMesh(model, form - 1);
            }
            s_meshes[path] = mesh;
            return mesh;
        }

        private void Update()
        {
            UpdateSwing();
            if (_popTime < 0f || _weapon == null) return;
            _popTime += Time.deltaTime;
            float t = _popTime / PopDuration;
            if (t >= 1f)
            {
                _popTime = -1f;
                _weapon.localScale = Vector3.one;
                return;
            }
            // İlk çıkışta 0'dan taşarak büyür; form değişiminde bir an şişer.
            float s = _popFromZero
                ? 1f - Mathf.Pow(1f - t, 3f) + Mathf.Sin(t * Mathf.PI) * 0.25f
                : 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            _weapon.localScale = Vector3.one * s;
        }
    }
}

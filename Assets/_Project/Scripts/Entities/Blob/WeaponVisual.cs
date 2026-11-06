using System.Collections.Generic;
using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // 2026-11-06: Silah Gücü'nün görünür silahı. Modeller zorse-blender/build_weapons.py → Resources/FX/Weapons/G_<Model>_<form>.
    // Form, skilin yeni mekaniğiyle değişir (WeaponUpgradeEffect hesaplar): 1 temel · 2 çift hedef · 3 üçlü hedef · 4 son form.
    // Topik/Mıknato: Silah Gücü alınana kadar silah yok, sonra sol yanda belirir. Mermo: baştan form 1 (iki yanda keleş).
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

        public void Setup(Renderer host, string model, bool visibleFromStart)
        {
            if (_weapon != null) Destroy(_weapon.gameObject);
            _weapon = null;
            _form = -1;
            _popTime = -1f;
            _model = model;
            if (host == null || string.IsNullOrEmpty(model)) return;

            var go = new GameObject("Weapon");
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

        // form 0 = gizli, 1..4 = G_<Model>_<form>.
        public void SetForm(int form) => SetForm(form, pop: true);

        private void SetForm(int form, bool pop)
        {
            if (_weapon == null || form == _form) return;
            bool wasHidden = _form <= 0;
            _form = form;
            Mesh mesh = form > 0 ? LoadMesh(_model, form) : null;
            _filter.sharedMesh = mesh;
            _renderer.enabled = mesh != null;
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

        private static Mesh LoadMesh(string model, int form)
        {
            string path = $"{Folder}{model}_{form}";
            if (s_meshes.TryGetValue(path, out Mesh mesh)) return mesh;
            mesh = Resources.Load<Mesh>(path);
            if (mesh == null) Debug.LogWarning($"[WeaponVisual] Resources/{path} bulunamadı.");
            s_meshes[path] = mesh;
            return mesh;
        }

        private void Update()
        {
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

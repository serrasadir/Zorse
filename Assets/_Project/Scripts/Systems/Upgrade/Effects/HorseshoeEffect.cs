using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Nal Bumerangı (aktif, Mıknato'nun silahı). Sv.1 silahı kurar (1.6 sn'de 1 nal, 10 hasar/geçiş, menzil yüzeyden 6);
    // Sv.2 +4 hasar +1 menzil · Sv.3 2 nal · Sv.4 dönüşte coin/kalp toplar + atış hızı ×1.3 · Sv.5 tur + hasar ×1.5.
    // Model: Art/City/X_Horseshoe.fbx (Blender build_horseshoe.py), Skill Setini Kur bağlar.
    [CreateAssetMenu(fileName = "HorseshoeEffect", menuName = "BlobSurvivor/Effects/Nal Bumerangi")]
    public class HorseshoeEffect : UpgradeEffect
    {
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _range = 6f;
        [SerializeField] private float _interval = 1.6f;

        [Header("Kilometre taşları")]
        [SerializeField] private float _damagePerLevel2 = 4f;
        [SerializeField] private float _rangePerLevel2 = 1f;
        [SerializeField] private float _collectFireRate = 1.3f;
        [SerializeField] private float _orbitDamageScale = 1.5f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            HorseshoeWeapon weapon = blobRoot.GetComponentInChildren<HorseshoeWeapon>();
            if (weapon == null)
            {
                // Run başına tek seferlik kurulum; restart'ta GameManager.ClearPreviousRunState siler.
                var go = new GameObject("HorseshoeWeapon");
                go.transform.SetParent(blobRoot.transform, false);
                weapon = go.AddComponent<HorseshoeWeapon>();
                weapon.Setup(_mesh, _material, _damage, _range, _interval);
            }

            if (level == 2) weapon.AddDamage(_damagePerLevel2, _rangePerLevel2);
            else if (level == 3) weapon.SetCount(2);
            else if (level == 4) weapon.EnableCollect(_collectFireRate);
            else if (level == 5) weapon.EnableOrbit(_orbitDamageScale);
        }

#if UNITY_EDITOR
        public void EditorSetModel(Mesh mesh, Material material) { _mesh = mesh; _material = material; }
#endif
    }
}

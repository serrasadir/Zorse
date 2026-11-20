using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Çöp Fırlatma (aktif, Cano'nun silahı). Yediği cansız nesneler cephane olur, çöpü yay çizerek en yakın düşmana atar.
    // Sv.1 12 hasar, alan 1.2, menzil yüzeyden 7, depo 10 · Sv.2 +4 hasar, depo 15 · Sv.3 her atışta 2 çöp ·
    // Sv.4 Medium+ lokma 3 çöp + düştüğü yerde 2 sn leke (%40 yavaşlatır) · Sv.5 atış hızı ×2, çarpınca 3 parçaya bölünür.
    // Modeller yerdeki yiyeceklerden (Art/City/Litter/J_*), Skill Setini Kur bağlar.
    [CreateAssetMenu(fileName = "TrashThrowEffect", menuName = "BlobSurvivor/Effects/Cop Firlatma")]
    public class TrashThrowEffect : UpgradeEffect
    {
        [SerializeField] private Mesh[] _meshes;
        [SerializeField] private Material _material;
        [SerializeField] private float _damage = 12f;
        [SerializeField] private float _radius = 1.2f;
        [SerializeField] private float _range = 7f;
        [SerializeField] private int _ammoCap = 10;

        [Header("Kilometre taşları")]
        [SerializeField] private float _damagePerLevel2 = 4f;
        [SerializeField] private int _ammoCapLevel2 = 15;
        [SerializeField] private int _bigBiteAmmo = 3;
        [SerializeField] private float _puddleSlow = 0.6f; // hız çarpanı (%40 yavaş)
        [SerializeField] private float _puddleTime = 2f;
        [SerializeField] private float _finalFireRate = 2f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = LevelOf(data);
            TrashWeapon weapon = blobRoot.GetComponentInChildren<TrashWeapon>();
            if (weapon == null)
            {
                // Run başına tek seferlik kurulum; restart'ta GameManager.ClearPreviousRunState siler.
                var go = new GameObject("TrashWeapon");
                go.transform.SetParent(blobRoot.transform, false);
                weapon = go.AddComponent<TrashWeapon>();
                weapon.Setup(_meshes, _material, _damage, _radius, _range, _ammoCap);
            }

            if (level == 2) weapon.Upgrade(_damagePerLevel2, _ammoCapLevel2);
            else if (level == 3) weapon.SetCount(2);
            else if (level == 4) weapon.EnableBigBite(_bigBiteAmmo, _puddleSlow, _puddleTime);
            else if (level == 5) weapon.EnableFinalForm(_finalFireRate);
        }

#if UNITY_EDITOR
        public void EditorSetModels(Mesh[] meshes, Material material) { _meshes = meshes; _material = material; }
#endif
    }
}

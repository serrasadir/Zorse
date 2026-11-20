#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Core
{
    // 2026-11-04: sadece Editor'da çalışan test tuşları (build'e hiç girmez — dosyanın tamamı #if UNITY_EDITOR).
    // Sahne kurulumu yok; Play'e basınca kendini oluşturur.
    //   M  +500 kütle (XP vermez, kart ekranı açılmaz) — helikopteri yutacak boya hızlı gelmek için
    //   B  aktif final boss'a canının %25'i kadar hasar — irtifa kademelerini hızlı görmek için
    //   H  canı doldur
    //   N  sıradaki boss aşamasını hemen çıkar (aktif boss ödülsüz kalkar) — miniboss → miniboss 2 → helikopter
    //   V  Vakum skill'i +1 seviye (kart ekranı açılmaz)
    //   K  Kara Delik: Vakum max + Sindirim 1 + evrim (evrim anı efekti oynar)
    //   X  Lamba Direği: Sopa max + Yara Öfkesi 1 + evrim
    //   F  Bayram Gecesi: Havai Fişek max + Tetikçi 1 + evrim
    //   R  Sabun Sörfü · Q  Hurda Kalkanı · E  Geğirme (aktif max + eş 1 + evrim)
    //   Z  Zırh skill'i +1 seviye (kalkan; Sv.3 kırılınca dokunulmazlık, Sv.5 ikinci katman)
    //   G  Sopa +1 seviye (görünür sopa: Sv.1, 2, 4, 5'te form değişir)
    //   J  Hız +1 seviye (Sv.1 kaykay, Sv.3 alevli kaykay, Sv.5 roket)
    //   C  +500 Market kredisi (kayda yazılır; lobide/Market'te de çalışır)
    public class DevCheats : MonoBehaviour
    {
        private const float MassStep = 500f;

        private BlobGrowth _growth;
        private BlobHealth _health;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            GameObject go = new GameObject("DevCheats (Editor)") { hideFlags = HideFlags.DontSave };
            go.AddComponent<DevCheats>();
            Debug.Log("[DevCheats] M: +500 kütle · B: final boss −%25 can · H: canı doldur · N: sıradaki boss · L: kuşatma · Y: çevik kuvvet + motorlu polis · T: dönme dolap dönüşümü · O: yeni görev · P: görevi bitir · V: Vakum +1 · K: Kara Delik · X: Lamba Direği · F: Bayram Gecesi · R: Sabun Sörfü · Q: Hurda Kalkanı · E: Geğirme · Z: Zırh +1 · U: Havai Fişek +1 · C: +500 coin (sadece Editor)");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (_growth == null) _growth = FindAnyObjectByType<BlobGrowth>();
            if (_health == null && _growth != null) _health = _growth.GetComponent<BlobHealth>();

            if (keyboard.cKey.wasPressedThisFrame && BlobSurvivor.Systems.Meta.MetaProgression.Instance != null)
            {
                var meta = BlobSurvivor.Systems.Meta.MetaProgression.Instance;
                meta.DevAddCredit(500);
                Debug.Log($"[DevCheats] Coin {meta.Credit}");
            }

            if (keyboard.mKey.wasPressedThisFrame && _growth != null)
            {
                _growth.DebugAddMassWithoutXP(MassStep);
                Debug.Log($"[DevCheats] Kütle {_growth.CurrentMass:F0}, tier {_growth.CurrentTier}, ölçek ~{_growth.SmoothScale:F2} (hedefe yumuşak büyür)");
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                FinalBossController boss = FinalBossController.Active;
                EnemyBase enemy = boss != null ? boss.GetComponent<EnemyBase>() : null;
                if (enemy == null)
                    Debug.Log("[DevCheats] Aktif final boss yok.");
                else
                {
                    enemy.TakeDamage(enemy.MaxHealth * 0.25f);
                    Debug.Log($"[DevCheats] Final boss can {enemy.CurrentHealth:F0}/{enemy.MaxHealth:F0}");
                }
            }

            if (keyboard.nKey.wasPressedThisFrame && BossSpawner.Instance != null)
                Debug.Log("[DevCheats] " + BossSpawner.Instance.DebugSpawnNextStage());

            if (keyboard.vKey.wasPressedThisFrame && BlobSurvivor.Systems.UpgradeSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.UpgradeSystem.Instance.DebugGrant("vacuum"));

            if (keyboard.kKey.wasPressedThisFrame) DebugEvolution("vacuum", "Sindirim");
            if (keyboard.xKey.wasPressedThisFrame) DebugEvolution("weapon", "yara_ofkesi");
            if (keyboard.fKey.wasPressedThisFrame) DebugEvolution("firework", "tetikci");
            if (keyboard.rKey.wasPressedThisFrame) DebugEvolution("kaygan_zemin", "speed_boost");
            if (keyboard.qKey.wasPressedThisFrame) DebugEvolution("geri_donusum", "damage_reduction");
            if (keyboard.eKey.wasPressedThisFrame) DebugEvolution("sindirim_gazi", "hizli_metabolizma");

            if (keyboard.zKey.wasPressedThisFrame && BlobSurvivor.Systems.UpgradeSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.UpgradeSystem.Instance.DebugGrant("damage_reduction"));

            if (keyboard.gKey.wasPressedThisFrame && BlobSurvivor.Systems.UpgradeSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.UpgradeSystem.Instance.DebugGrant("weapon"));

            if (keyboard.uKey.wasPressedThisFrame && BlobSurvivor.Systems.UpgradeSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.UpgradeSystem.Instance.DebugGrant("firework"));

            if (keyboard.jKey.wasPressedThisFrame && BlobSurvivor.Systems.UpgradeSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.UpgradeSystem.Instance.DebugGrant("speed_boost"));

            if (keyboard.lKey.wasPressedThisFrame)
            {
                EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
                if (spawner != null) Debug.Log("[DevCheats] " + spawner.DebugEncircle());
            }

            if (keyboard.yKey.wasPressedThisFrame)
            {
                EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
                if (spawner != null) Debug.Log("[DevCheats] " + spawner.DebugSpawnSpecialTypes());
            }

            if (keyboard.iKey.wasPressedThisFrame) DayNightCycle.Instance?.DebugAdvance(180f); // gün 3 dk ileri
            if (keyboard.tKey.wasPressedThisFrame && _growth != null)
                FerrisWheelForm.Begin(_growth.gameObject);

            if (keyboard.oKey.wasPressedThisFrame && BlobSurvivor.Systems.MissionSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.MissionSystem.Instance.DebugStartNext());

            if (keyboard.pKey.wasPressedThisFrame && BlobSurvivor.Systems.MissionSystem.Instance != null)
                Debug.Log("[DevCheats] " + BlobSurvivor.Systems.MissionSystem.Instance.DebugComplete());

            if (keyboard.hKey.wasPressedThisFrame && _health != null)
                _health.Heal(_health.MaxHealth);
        }

        // Aktif skill'i max'a, eşi Sv.1'e çıkarır; evrim kendiliğinden gelir (evrim anı oynar).
        private static void DebugEvolution(string active, string partner)
        {
            var upgrades = BlobSurvivor.Systems.UpgradeSystem.Instance;
            if (upgrades == null) return;
            for (int i = 0; i < 5; i++) upgrades.DebugGrant(active);
            Debug.Log("[DevCheats] " + upgrades.DebugGrant(partner) + " → evrim anı");
        }
    }
}
#endif

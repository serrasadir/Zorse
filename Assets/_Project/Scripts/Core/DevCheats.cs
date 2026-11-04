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
            Debug.Log("[DevCheats] M: +500 kütle · B: final boss −%25 can · H: canı doldur (sadece Editor)");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (_growth == null) _growth = FindAnyObjectByType<BlobGrowth>();
            if (_health == null && _growth != null) _health = _growth.GetComponent<BlobHealth>();

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

            if (keyboard.hKey.wasPressedThisFrame && _health != null)
                _health.Heal(_health.MaxHealth);
        }
    }
}
#endif

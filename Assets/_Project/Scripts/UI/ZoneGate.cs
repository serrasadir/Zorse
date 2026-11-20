using System.Collections.Generic;
using BlobSurvivor.Core;
using BlobSurvivor.Entities;
using TMPro;
using UnityEngine;

namespace BlobSurvivor.UI
{
    // Kale haritasında bölge kapısı (CastleSceneBuilder kapı örneklerine ekler). Üstünde boy işareti: "ORTA BOY" (yetmiyor)
    // ya da "YE VE GEÇ!" (yetiyor, yeşil ve zıplar). GateGuide oku ve küçük harita sıradaki kapıyı buradan bulur.
    // Kapı yenince obje kapanır; kayıt Awake/OnDestroy'da tutulur ki ok hâlâ geçidi gösterebilsin.
    public class ZoneGate : MonoBehaviour
    {
        public static readonly List<ZoneGate> All = new List<ZoneGate>();

        [SerializeField] private int _zone;          // bu kapı hangi bölgenin çıkışı (0 = köy)
        [SerializeField] private TMP_Text _sign;

        private static readonly Color LockedColor = new Color(1f, 0.93f, 0.75f);
        private static readonly Color ReadyColor = new Color(0.55f, 1f, 0.45f);
        private ConsumableBase _consumable;
        private Vector3 _signScale;
        private bool _ready;

        public int Zone => _zone;
        public Vector3 Position => transform.position;
        public BlobTier RequiredTier => _consumable != null ? _consumable.RequiredTier : BlobTier.Little;
        public bool Ready => _ready;

        private void Awake()
        {
            _consumable = GetComponent<ConsumableBase>();
            if (_sign != null)
            {
                _signScale = _sign.transform.localScale;
                Camera cam = Camera.main;
                if (cam != null) _sign.transform.rotation = cam.transform.rotation; // kamera dönmüyor: bir kez
            }
            All.Add(this);
        }

        private void OnDestroy() => All.Remove(this);

        private void OnEnable()
        {
            GameEvents.OnBlobTierChanged += Refresh;
            Refresh(GameEvents.CurrentBlobTier);
        }

        private void OnDisable() => GameEvents.OnBlobTierChanged -= Refresh;

        private void Refresh(BlobTier tier)
        {
            _ready = tier >= RequiredTier;
            if (_sign == null) return;
            _sign.text = _ready ? "YE VE GEÇ!" : $"{TierNames.Get(RequiredTier)} BOY";
            _sign.color = _ready ? ReadyColor : LockedColor;
            if (!_ready) _sign.transform.localScale = _signScale;
        }

        private void Update()
        {
            if (!_ready || _sign == null) return;
            float s = 1f + Mathf.Sin(Time.time * 6f) * 0.08f; // hazırken nabız
            _sign.transform.localScale = _signScale * s;
        }

        // Blob'un bölgesinin çıkış kapısı (son bölgede yok).
        public static ZoneGate NextFor(Vector3 position)
        {
            int zone = MapRegions.ZoneOf(position);
            foreach (ZoneGate g in All)
                if (g != null && g._zone == zone) return g;
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetup(int zone, TMP_Text sign)
        {
            _zone = zone;
            _sign = sign;
        }
#endif
    }
}

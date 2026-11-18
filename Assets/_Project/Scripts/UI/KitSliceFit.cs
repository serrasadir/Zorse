using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.UI
{
    // Yalnız yatay 9-slice'lı kit parçaları (bar, hap, rozet, kurdele): kenar payını Image'ın GERÇEK yüksekliğine göre
    // ölçekler (çapalarla uzayan barlarda sizeDelta yükseklik değildir). Boyut değişince yeniden; her kare çalışmaz.
    [RequireComponent(typeof(Image))]
    public class KitSliceFit : MonoBehaviour
    {
        private Image _image;

        private void Awake() => _image = GetComponent<Image>();
        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();

        public void Fit()
        {
            if (_image == null) _image = GetComponent<Image>();
            Sprite s = _image != null ? _image.sprite : null;
            if (s == null) return;
            float h = ((RectTransform)transform).rect.height;
            if (h > 1f) _image.pixelsPerUnitMultiplier = s.rect.height / h;
        }
    }
}

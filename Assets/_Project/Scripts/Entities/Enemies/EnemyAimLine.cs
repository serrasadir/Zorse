using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.Entities.Enemies
{
    // Elit polisin nişan lazeri. LineRenderer düşman başına bir kez oluşturulur ve pool'la birlikte
    // yeniden kullanılır; sadece nişan alınırken açıktır.
    public class EnemyAimLine
    {
        private readonly LineRenderer _line;

        public EnemyAimLine(Transform owner, Material material)
        {
            GameObject lineObject = new GameObject("AimLine");
            lineObject.transform.SetParent(owner, false);

            _line = lineObject.AddComponent<LineRenderer>();
            _line.sharedMaterial = material;
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.enabled = false;
        }

        public void Show(Vector3 start, Vector3 end, float width)
        {
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
            _line.startWidth = width;
            _line.endWidth = width;
            _line.enabled = true;
        }

        public void Hide() => _line.enabled = false;
    }
}

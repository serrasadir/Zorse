using System;
using UnityEngine;

namespace BlobSurvivor.Data
{
    // Yürüyen yaya görünümü: bir varyant mesh'inin gövde + bacak + kol parçaları ve eklem noktaları (mesh-yerel).
    // Tools → Blob.io → Yaya Yürüyüş Parçalarını Kur üretir (Art/City/People/Rigs). Parçalar Model'in child'ı olur,
    // eklem etrafında SwingAxis'te sallanır (PedestrianController). Rig'i olmayan varyant eskisi gibi tek mesh.
    [Serializable]
    public class PedestrianRig
    {
        public Mesh Source;               // VariantMeshes'teki karşılığı
        public Mesh Body;
        public Mesh LegL, LegR, ArmL, ArmR;
        public Vector3 HipL, HipR, ShoulderL, ShoulderR;
        public Vector3 SwingAxis = Vector3.right; // mesh-yerel sol-sağ ekseni (ileri-geri sallanma)
    }
}

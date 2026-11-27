using UnityEngine;
using UnityEngine.InputSystem;

namespace Crownreach
{
    [RequireComponent(typeof(Camera))]
    public sealed class CrownreachCamera : MonoBehaviour
    {
        public Transform[] districts;
        private Camera lens;
        private Vector3 focus;
        private float yaw, pitch = 45f;
        private int selected;
        private bool framed;
        private float lastAspect;

        public void Frame(int index)
        {
            if (districts == null || districts.Length != 4) return;
            lens = GetComponent<Camera>();
            lens.orthographic = true;
            selected = index;
            framed = true;
            Bounds[] bounds = new Bounds[4];
            for (int i = 0; i < districts.Length; i++)
            {
                var renderers = districts[i].GetComponentsInChildren<MeshRenderer>();
                bounds[i] = renderers[0].bounds;
                foreach (var r in renderers) bounds[i].Encapsulate(r.bounds);
            }
            Vector3 south = bounds[0].center - bounds[3].center;
            south.y = 0;
            south.Normalize();
            Vector3 offset = Quaternion.AngleAxis(-23f, Vector3.up) * south;
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            pitch = 44f;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            Bounds combined = index == 0 ? bounds[0] : bounds[index-1];
            if (index == 0) foreach (var b in bounds) combined.Encapsulate(b);
            focus = combined.center;
            float extent = 0;
            for (int i = 0; i < districts.Length; i++)
            {
                if (index != 0 && index != i+1) continue;
                foreach (var r in districts[i].GetComponentsInChildren<MeshRenderer>())
                {
                    var b = r.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 p = b.center + Vector3.Scale(b.extents, new Vector3(
                            (corner & 1)==0?-1:1,(corner & 2)==0?-1:1,(corner & 4)==0?-1:1)) - focus;
                        extent = Mathf.Max(extent, Mathf.Abs(Vector3.Dot(p,transform.up)),
                            Mathf.Abs(Vector3.Dot(p,transform.right)) / lens.aspect);
                    }
                }
            }
            lens.orthographicSize = Mathf.Max(4,extent*1.07f);
            lastAspect = lens.aspect;
            PositionCamera();
        }
        void Start() => Frame(0);
        void PositionCamera()
        {
            transform.rotation = Quaternion.Euler(pitch,yaw,0);
            transform.position = focus-transform.forward*155;
        }
        void LateUpdate()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit0Key.wasPressedThisFrame) Frame(0);
                if (kb.digit1Key.wasPressedThisFrame) Frame(1);
                if (kb.digit2Key.wasPressedThisFrame) Frame(2);
                if (kb.digit3Key.wasPressedThisFrame) Frame(3);
                if (kb.digit4Key.wasPressedThisFrame) Frame(4);
            }
            if (framed && Mathf.Abs(lastAspect-lens.aspect)>.01f) Frame(selected);
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue();
            if (mouse.middleButton.isPressed)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
                focus -= (transform.right*delta.x+forward*delta.y/Mathf.Sin(pitch*Mathf.Deg2Rad))
                    * (2*lens.orthographicSize/Mathf.Max(1,lens.pixelHeight));
                framed = false;
            }
            else if (mouse.rightButton.isPressed)
            {
                yaw += delta.x*.2f;
                pitch = Mathf.Clamp(pitch-delta.y*.2f,20,82);
                framed = false;
            }
            float wheel = mouse.scroll.ReadValue().y;
            if (wheel != 0)
            {
                lens.orthographicSize = Mathf.Clamp(lens.orthographicSize*Mathf.Exp(-wheel*.0015f),2.5f,180);
                framed = false;
            }
            PositionCamera();
        }
    }
}

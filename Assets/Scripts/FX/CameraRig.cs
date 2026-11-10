using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Câmera de cima (estilo GTA 1 / Dead Ops): segue o jogador com antecipação
    /// para onde ele mira, treme em tiros e explosões e não sai do mapa.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }

        public Transform target;
        public Vector2 lookAhead;
        public Rect bounds;
        private float trauma;
        private Vector3 smoothPos;
        private Camera cam;

        private void Awake()
        {
            I = this;
            cam = GetComponent<Camera>();
        }

        public static void Shake(float amount)
        {
            if (I != null && SaveData.ScreenShake)
            {
                I.trauma = Mathf.Min(1f, I.trauma + amount);
            }
        }

        public void SnapTo(Vector2 p)
        {
            smoothPos = new Vector3(p.x, p.y, -10f);
            transform.position = smoothPos;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 want = (Vector2)target.position + lookAhead;
            want.z = -10f;
            smoothPos = Vector3.Lerp(smoothPos, want, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            float halfH = cam != null ? cam.orthographicSize : 6.75f;
            float halfW = halfH * (cam != null ? cam.aspect : 16f / 9f);
            Vector3 p = smoothPos;
            if (bounds.width > halfW * 2f) p.x = Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            if (bounds.height > halfH * 2f) p.y = Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);

            if (trauma > 0f)
            {
                float s = trauma * trauma * 0.35f;
                p += new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0f);
                trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 2.2f);
            }

            transform.position = p;
        }
    }
}

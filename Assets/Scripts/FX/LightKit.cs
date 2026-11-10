using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Sanatorio
{
    /// <summary>
    /// Cria luzes 2D a partir dos prefabs em Resources/Prefabs (feitos pelo
    /// GameSetup, já apontando para a camada de ordenação certa).
    /// </summary>
    public static class LightKit
    {
        private static GameObject pointPrefab;
        private static GameObject globalPrefab;

        public static Light2D Point(Transform parent, Vector2 pos, float radius, float intensity, Color color, float innerRatio = 0.25f)
        {
            if (pointPrefab == null) pointPrefab = Resources.Load<GameObject>("Prefabs/LightPoint");
            GameObject go = pointPrefab != null ? Object.Instantiate(pointPrefab, parent) : new GameObject("LightPoint", typeof(Light2D));
            if (pointPrefab == null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light2D l = go.GetComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * innerRatio;
            l.intensity = intensity;
            l.color = color;
            l.falloffIntensity = 0.6f;
            return l;
        }

        public static Light2D Spot(Transform parent, float radius, float intensity, Color color, float angle)
        {
            Light2D l = Point(parent, parent.position, radius, intensity, color, 0.1f);
            l.transform.localPosition = Vector3.zero;
            l.pointLightOuterAngle = angle;
            l.pointLightInnerAngle = angle * 0.55f;
            return l;
        }

        public static Light2D Global(Transform parent, float intensity, Color color)
        {
            if (globalPrefab == null) globalPrefab = Resources.Load<GameObject>("Prefabs/LightGlobal");
            GameObject go = globalPrefab != null ? Object.Instantiate(globalPrefab, parent) : new GameObject("LightGlobal", typeof(Light2D));
            Light2D l = go.GetComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.intensity = intensity;
            l.color = color;
            return l;
        }
    }

    /// <summary>Lâmpada que pisca de vez em quando (mais antes da energia).</summary>
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 0.9f;
        public float flickerChance = 0.02f;
        public bool on = true;
        private Light2D lightComp;
        private float dip;

        private void Awake()
        {
            lightComp = GetComponent<Light2D>();
        }

        private void Update()
        {
            if (lightComp == null) return;
            if (!on)
            {
                lightComp.intensity = Mathf.MoveTowards(lightComp.intensity, 0f, Time.deltaTime * 4f);
                return;
            }

            if (dip <= 0f && Random.value < flickerChance * Time.deltaTime * 60f)
            {
                dip = Random.Range(0.05f, 0.25f);
            }

            float target = baseIntensity;
            if (dip > 0f)
            {
                dip -= Time.deltaTime;
                target = baseIntensity * (Random.value < 0.5f ? 0.15f : 0.6f);
            }

            lightComp.intensity = Mathf.MoveTowards(lightComp.intensity, target, Time.deltaTime * 8f);
        }
    }
}

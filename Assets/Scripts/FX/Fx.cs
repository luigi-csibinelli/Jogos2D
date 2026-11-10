using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sanatorio
{
    /// <summary>
    /// Efeitos visuais: dois ParticleSystems compartilhados (um que recebe luz,
    /// para sangue e poeira; um aditivo, para faíscas e fogo), traçantes de bala,
    /// manchas no chão e luzes de clarão. Tudo em pool, nada é instanciado no tiro.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I { get; private set; }

        private ParticleSystem litSystem;
        private ParticleSystem glowSystem;
        private readonly Queue<LineRenderer> tracers = new Queue<LineRenderer>();
        private readonly Queue<SpriteRenderer> decals = new Queue<SpriteRenderer>();
        private readonly Queue<UnityEngine.Rendering.Universal.Light2D> flashes = new Queue<UnityEngine.Rendering.Universal.Light2D>();
        private Material tracerMat;

        public static void Create(Transform parent)
        {
            GameObject go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<Fx>();
            I.Build();
        }

        private void Build()
        {
            litSystem = MakeSystem("Particles_Lit", Res.Lit, Order.Fx - 1);
            glowSystem = MakeSystem("Particles_Glow", Res.Additive != null ? Res.Additive : Res.Unlit, Order.Fx);

            tracerMat = Res.Additive != null ? Res.Additive : Res.Unlit;
            for (int i = 0; i < 40; i++)
            {
                GameObject t = new GameObject("Tracer");
                t.transform.SetParent(transform, false);
                LineRenderer lr = t.AddComponent<LineRenderer>();
                lr.sharedMaterial = tracerMat;
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.widthMultiplier = 1f / 16f;
                lr.sortingOrder = Order.Fx;
                lr.numCapVertices = 0;
                lr.enabled = false;
                tracers.Enqueue(lr);
            }

            for (int i = 0; i < 90; i++)
            {
                GameObject d = new GameObject("Decal");
                d.transform.SetParent(transform, false);
                SpriteRenderer sr = Res.AddSprite(d, "Blood0", Order.Decal);
                sr.enabled = false;
                decals.Enqueue(sr);
            }

            for (int i = 0; i < 8; i++)
            {
                UnityEngine.Rendering.Universal.Light2D l = LightKit.Point(transform, Vector2.zero, 3f, 0f, Color.white);
                l.gameObject.name = "Flash";
                flashes.Enqueue(l);
            }
        }

        private ParticleSystem MakeSystem(string name, Material mat, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = false;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            ParticleSystem.LimitVelocityOverLifetimeModule lim = ps.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.drag = 6f;
            lim.multiplyDragByParticleSize = false;
            lim.multiplyDragByParticleVelocity = true;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            Material m = new Material(mat);
            m.mainTexture = Res.Sprite("Px").texture;
            r.sharedMaterial = m;
            r.sortingOrder = order;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.alignment = ParticleSystemRenderSpace.View;
            r.minParticleSize = 0f;

            ps.Play();
            return ps;
        }

        // ------------------------------------------------------------ partículas
        public static void Burst(Vector2 pos, Color color, int count, float speed, float life, float size, bool glow = false, Vector2 dir = default, float cone = 360f)
        {
            if (I == null) return;
            ParticleSystem ps = glow ? I.glowSystem : I.litSystem;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            float baseAngle = dir == Vector2.zero ? 0f : Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float a = (baseAngle + Random.Range(-cone * 0.5f, cone * 0.5f)) * Mathf.Deg2Rad;
                float s = speed * Random.Range(0.35f, 1f);
                p.position = pos + Random.insideUnitCircle * 0.08f;
                p.velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * s;
                p.startLifetime = life * Random.Range(0.6f, 1.2f);
                p.startSize = size * Random.Range(0.7f, 1.3f);
                p.startColor = color;
                ps.Emit(p, 1);
            }
        }

        public static void Blood(Vector2 pos, Vector2 dir, int count = 8)
        {
            Burst(pos, new Color(0.55f, 0.06f, 0.08f), count, 4f, 0.45f, 2f / 16f, false, dir, 70f);
        }

        public static void Sparks(Vector2 pos, Vector2 dir, int count = 6, Color? color = null)
        {
            Burst(pos, color ?? new Color(1f, 0.85f, 0.4f), count, 5f, 0.22f, 1.5f / 16f, true, dir, 100f);
        }

        private static float critSoundReady;

        /// <summary>Acerto crítico: estouro claro e um "tim" agudo (com limite de repetição).</summary>
        public static void Crit(Vector2 pos, bool loud = true)
        {
            Burst(pos, new Color(1f, 0.95f, 0.55f), loud ? 14 : 4, 6f, 0.25f, 2f / 16f, true);
            Burst(pos, Color.white, loud ? 6 : 2, 3f, 0.15f, 3f / 16f, true);
            if (loud) Flash(pos, new Color(1f, 0.9f, 0.5f), 2.2f, 1.4f, 0.08f);
            if (Time.time >= critSoundReady)
            {
                critSoundReady = Time.time + (loud ? 0.06f : 0.25f);
                AudioManager.PlayAt("Crit", pos, loud ? 0.7f : 0.35f, 0.08f);
            }
        }

        /// <summary>Raio caindo do céu (onde nasce um cão): zigue-zague, clarão e trovão.</summary>
        public static void Lightning(Vector2 pos)
        {
            Vector2 a = pos + new Vector2(Random.Range(-1f, 1f), 7f);
            for (int i = 0; i < 6; i++)
            {
                Vector2 b = Vector2.Lerp(pos + new Vector2(0f, 7f), pos, (i + 1) / 6f) + (i < 5 ? new Vector2(Random.Range(-0.7f, 0.7f), 0f) : Vector2.zero);
                Tracer(a, b, new Color(0.75f, 0.85f, 1f), 0.18f);
                a = b;
            }

            Flash(pos, new Color(0.7f, 0.8f, 1f), 7f, 2.2f, 0.35f);
            Burst(pos, new Color(0.7f, 0.85f, 1f), 18, 5f, 0.3f, 2f / 16f, true);
            Decal(pos, "Scorch", 8f);
            CameraRig.Shake(0.25f);
            AudioManager.PlayAt("Thunder", pos, 1f, 0.05f);
        }

        public static void Dust(Vector2 pos, int count = 10, Color? color = null)
        {
            Burst(pos, color ?? new Color(0.6f, 0.55f, 0.48f), count, 2.5f, 0.6f, 2f / 16f);
        }

        public static void Explosion(Vector2 pos, float radius)
        {
            Burst(pos, new Color(1f, 0.75f, 0.25f), 26, radius * 4f, 0.35f, 3f / 16f, true);
            Burst(pos, new Color(1f, 0.35f, 0.1f), 18, radius * 3f, 0.5f, 4f / 16f, true);
            Burst(pos, new Color(0.25f, 0.22f, 0.22f), 20, radius * 2f, 1.1f, 4f / 16f);
            Flash(pos, new Color(1f, 0.7f, 0.35f), radius * 2.6f, 1.6f, 0.25f);
            Decal(pos, "Scorch", 18f);
            CameraRig.Shake(0.35f);
        }

        // ------------------------------------------------------------ traçantes / clarões
        public static void Tracer(Vector2 from, Vector2 to, Color color, float time = 0.05f)
        {
            if (I == null) return;
            LineRenderer lr = I.tracers.Dequeue();
            I.tracers.Enqueue(lr);
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.15f);
            lr.enabled = true;
            I.StartCoroutine(I.HideLater(lr, time));
        }

        private IEnumerator HideLater(LineRenderer lr, float t)
        {
            yield return new WaitForSeconds(t);
            lr.enabled = false;
        }

        public static void Flash(Vector2 pos, Color color, float radius, float intensity, float time)
        {
            if (I == null) return;
            UnityEngine.Rendering.Universal.Light2D l = I.flashes.Dequeue();
            I.flashes.Enqueue(l);
            l.transform.position = pos;
            l.color = color;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.2f;
            I.StartCoroutine(I.FadeLight(l, intensity, time));
        }

        private IEnumerator FadeLight(UnityEngine.Rendering.Universal.Light2D l, float intensity, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.deltaTime;
                l.intensity = intensity * (1f - t / time);
                yield return null;
            }

            l.intensity = 0f;
        }

        // ------------------------------------------------------------ manchas no chão
        public static void Decal(Vector2 pos, string sprite, float life, float rotation = -1f, Color? tint = null)
        {
            if (I == null) return;
            SpriteRenderer sr = I.decals.Dequeue();
            I.decals.Enqueue(sr);
            sr.sprite = Res.Sprite(sprite);
            sr.transform.position = pos;
            sr.transform.rotation = Quaternion.Euler(0, 0, rotation < 0f ? Random.Range(0, 4) * 90f : rotation);
            sr.color = tint ?? Color.white;
            sr.enabled = true;
            I.StartCoroutine(I.FadeDecal(sr, life));
        }

        private IEnumerator FadeDecal(SpriteRenderer sr, float life)
        {
            Sprite mine = sr.sprite;
            Vector3 p = sr.transform.position;
            yield return new WaitForSeconds(life);
            float t = 0f;
            Color c = sr.color;
            while (t < 1.5f)
            {
                if (sr.sprite != mine || sr.transform.position != p) yield break;   // reaproveitada
                t += Time.deltaTime;
                sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t / 1.5f));
                yield return null;
            }

            if (sr.sprite == mine && sr.transform.position == p) sr.enabled = false;
        }
    }
}

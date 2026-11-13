using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Efek "juice": teks melayang (uang masuk), percikan, dan getar kamera.</summary>
    public class JuiceFX : MonoBehaviour
    {
        public static JuiceFX I;

        readonly List<GameObject> pooled = new List<GameObject>();
        Camera cam;
        float shakeTime, shakeAmount;

        void Awake() { I = this; }

        public void Init(Camera camera) { cam = camera; }

        public void PopText(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            GameObject go = new GameObject("popText");
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 60;
            tm.characterSize = 0.09f * scale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            Font f = FontProvider.Get();
            if (f != null) { tm.font = f; MeshRenderer mr = go.GetComponent<MeshRenderer>(); if (mr != null) mr.sharedMaterial = f.material; }
            FloatingText ft = go.AddComponent<FloatingText>();
            ft.cam = cam;
            ft.life = 1.35f;
        }

        public void Burst(Vector3 worldPos, Color color, int count = 12)
        {
            Material mat = MaterialLibrary.Emissive(color, 1.6f);
            for (int i = 0; i < count; i++)
            {
                GameObject go = BuildKit.Create(PrimitiveType.Cube, transform, "spark", Vector3.zero,
                    Vector3.one * Random.Range(0.05f, 0.11f), mat);
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.position = worldPos;
                Particle p = go.AddComponent<Particle>();
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.4f, 1.4f), Random.Range(-1f, 1f));
                p.velocity = dir.normalized * Random.Range(1.6f, 3.6f);
                p.life = Random.Range(0.45f, 0.85f);
                p.spin = new Vector3(Random.Range(-360, 360), Random.Range(-360, 360), Random.Range(-360, 360));
            }
        }

        public void MoneyPop(Vector3 worldPos, int amount)
        {
            PopText(worldPos, "+" + Catalog.Money(amount), MaterialLibrary.Palette.Money, 1.15f);
            Burst(worldPos, MaterialLibrary.Palette.Gold, 8);
        }

        public void Shake(float amount, float duration)
        {
            shakeAmount = Mathf.Max(shakeAmount, amount);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        void LateUpdate()
        {
            if (shakeTime > 0f && cam != null)
            {
                shakeTime -= Time.deltaTime;
                float k = Mathf.Clamp01(shakeTime) * shakeAmount;
                Vector3 off = new Vector3(Random.Range(-k, k), Random.Range(-k, k), 0f);
                cam.transform.position += off;
                shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, Time.deltaTime * 2f);
            }
        }
    }

    /// <summary>Teks melayang naik lalu menghilang.</summary>
    public class FloatingText : MonoBehaviour
    {
        public Camera cam;
        public float life = 1.2f;
        float t;
        TextMesh tm;
        Vector3 start;

        void Start()
        {
            tm = GetComponent<TextMesh>();
            start = transform.position;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = t / life;
            transform.position = start + Vector3.up * (k * 1.1f);
            if (tm != null)
            {
                Color c = tm.color;
                c.a = Mathf.Clamp01(1.4f - k * 1.4f);
                tm.color = c;
            }
            if (cam != null) transform.rotation = cam.transform.rotation;
            if (t >= life) Destroy(gameObject);
        }
    }

    /// <summary>Percikan kecil dengan gravitasi.</summary>
    public class Particle : MonoBehaviour
    {
        public Vector3 velocity;
        public Vector3 spin;
        public float life = 0.7f;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            velocity += Vector3.down * 9.8f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime);
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0f, t / life);
            if (t >= life) Destroy(gameObject);
        }
    }
}

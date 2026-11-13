using UnityEngine;

namespace Warteg
{
    /// <summary>Efek hujan sederhana (tanpa shader khusus) supaya tetap ringan di Android.</summary>
    public class RainFX : MonoBehaviour
    {
        Transform[] drops;
        float[] speed;
        Bounds area;
        bool active;

        public void Setup(int count, Bounds bounds)
        {
            area = bounds;
            drops = new Transform[count];
            speed = new float[count];
            Material mat = MaterialLibrary.Unlit(new Color(0.72f, 0.84f, 0.95f));
            for (int i = 0; i < count; i++)
            {
                GameObject d = BuildKit.Create(PrimitiveType.Cube, transform, "drop" + i, Vector3.zero,
                    new Vector3(0.025f, 0.5f, 0.025f), mat);
                Renderer r = d.GetComponent<Renderer>();
                if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d.transform.localPosition = RandomPos();
                drops[i] = d.transform;
                speed[i] = Random.Range(9f, 15f);
            }
            SetActive(false);
        }

        Vector3 RandomPos()
        {
            return new Vector3(
                Random.Range(area.min.x, area.max.x),
                Random.Range(area.center.y, area.max.y),
                Random.Range(area.min.z, area.max.z));
        }

        public void SetActive(bool on)
        {
            active = on;
            if (drops != null)
                for (int i = 0; i < drops.Length; i++)
                    if (drops[i] != null) drops[i].gameObject.SetActive(on);
        }

        void Update()
        {
            if (!active || drops == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < drops.Length; i++)
            {
                Transform d = drops[i];
                if (d == null) continue;
                d.position += Vector3.down * speed[i] * dt;
                if (d.position.y < area.min.y)
                {
                    Vector3 p = RandomPos();
                    p.y = area.max.y;
                    d.position = p;
                }
            }
        }
    }
}

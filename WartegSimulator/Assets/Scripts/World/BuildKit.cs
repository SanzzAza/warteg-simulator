using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Helper untuk merakit objek 3D stylized dari primitive.
    /// Semua environment, perabot, dan karakter dibangun dengan ini sehingga
    /// project tidak membutuhkan asset model eksternal.
    /// </summary>
    public static class BuildKit
    {
        public static GameObject Create(PrimitiveType type, Transform parent, string name, Vector3 localPos,
                                        Vector3 scale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null && !keepCollider) Object.Destroy(col);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Color color,
                                     float smooth = 0.15f, bool collider = false)
        {
            return Create(PrimitiveType.Cube, parent, name, pos, size, MaterialLibrary.Lit(color, smooth), collider);
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false)
        {
            return Create(PrimitiveType.Cube, parent, name, pos, size, mat, collider);
        }

        public static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 size, Color color,
                                     float smooth = 0.3f, bool collider = false)
        {
            return Create(PrimitiveType.Cylinder, parent, name, pos, size, MaterialLibrary.Lit(color, smooth), collider);
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 size, Color color,
                                        float smooth = 0.35f, bool collider = false)
        {
            return Create(PrimitiveType.Sphere, parent, name, pos, size, MaterialLibrary.Lit(color, smooth), collider);
        }

        public static GameObject Capsule(Transform parent, string name, Vector3 pos, Vector3 size, Color color,
                                         bool collider = false)
        {
            return Create(PrimitiveType.Capsule, parent, name, pos, size, MaterialLibrary.Lit(color, 0.25f), collider);
        }

        /// <summary>Bidang datar (plane) dengan skala 1 unit = 1 meter.</summary>
        public static GameObject Quad(Transform parent, string name, Vector3 pos, Vector3 scale, Color color,
                                      bool collider = false)
        {
            GameObject go = Create(PrimitiveType.Quad, parent, name, pos, scale, MaterialLibrary.Lit(color, 0.1f), collider);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return go;
        }

        public static GameObject Empty(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static void AddBoxCollider(GameObject go, Vector3 size, Vector3 center)
        {
            BoxCollider bc = go.GetComponent<BoxCollider>();
            if (bc == null) bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = center;
        }

        // -----------------------------------------------------------------
        //  Teks 3D (papan nama, label harga)
        // -----------------------------------------------------------------
        public static TextMesh WorldText(Transform parent, string name, string text, Vector3 pos,
                                         float size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, float rotY = 0f)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            Font f = FontProvider.Get();
            if (f != null)
            {
                tm.font = f;
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = f.material;
            }
            return tm;
        }

        public static GameObject Plane(Transform parent, string name, Vector3 pos, Vector3 size, Color color, bool collider = true)
        {
            GameObject go = Create(PrimitiveType.Plane, parent, name, pos, size, MaterialLibrary.Lit(color, 0.12f), collider);
            return go;
        }

        /// <summary>Membuat kotak transparan tipis (kaca etalase) memakai material unlit cerah.</summary>
        public static GameObject GlassPanel(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            return Create(PrimitiveType.Cube, parent, name, pos, size,
                MaterialLibrary.Lit(new Color(0.72f, 0.86f, 0.92f), 0.85f), false);
        }
    }

    /// <summary>Font bawaan Unity untuk UI maupun teks 3D (tanpa perlu asset TMP).</summary>
    public static class FontProvider
    {
        static Font cached;

        public static Font Get()
        {
            if (cached != null) return cached;
            try { cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { cached = null; }
            if (cached == null)
            {
                try { cached = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { cached = null; }
            }
            if (cached == null) cached = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return cached;
        }
    }
}

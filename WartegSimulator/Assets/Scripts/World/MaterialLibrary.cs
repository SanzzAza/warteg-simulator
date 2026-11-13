using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Warteg
{
    /// <summary>
    /// Membuat & meng-cache material stylized. Otomatis memakai shader URP jika
    /// Universal Render Pipeline aktif, kalau tidak memakai shader Standard.
    /// </summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit;
        static Shader unlit;
        static Shader particle;

        public static bool UsingUrp
        {
            get { return GraphicsSettings.currentRenderPipeline != null; }
        }

        static Shader FindShader(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader s = Shader.Find(names[i]);
                if (s != null) return s;
            }
            return null;
        }

        static void EnsureShaders()
        {
            if (lit != null) return;
            if (UsingUrp)
            {
                lit = FindShader(new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard" });
                unlit = FindShader(new[] { "Universal Render Pipeline/Unlit", "Unlit/Color", "Standard" });
                particle = FindShader(new[] { "Universal Render Pipeline/Particles/Unlit", "Particles/Standard Unlit", "Sprites/Default", "Unlit/Color" });
            }
            else
            {
                lit = FindShader(new[] { "Standard", "Legacy Shaders/Diffuse" });
                unlit = FindShader(new[] { "Unlit/Color", "Standard" });
                particle = FindShader(new[] { "Particles/Standard Unlit", "Sprites/Default", "Unlit/Color" });
            }
            if (lit == null) lit = Shader.Find("Standard");
            if (unlit == null) unlit = lit;
            if (particle == null) particle = unlit;
        }

        public static Material Lit(Color color, float smoothness = 0.18f, float metallic = 0f)
        {
            EnsureShaders();
            string key = "lit_" + ColorKey(color) + "_" + smoothness.ToString("0.00") + "_" + metallic.ToString("0.00");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            m = new Material(lit);
            m.name = key;
            SetColor(m, color);
            SetFloat(m, "_Smoothness", smoothness, "_Glossiness");
            SetFloat(m, "_Metallic", metallic, "_Metallicness");
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        public static Material Emissive(Color color, float intensity = 1.4f)
        {
            EnsureShaders();
            string key = "emi_" + ColorKey(color) + "_" + intensity.ToString("0.0");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            m = new Material(lit);
            m.name = key;
            SetColor(m, color);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color)
        {
            EnsureShaders();
            string key = "unlit_" + ColorKey(color);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(unlit);
            m.name = key;
            SetColor(m, color);
            cache[key] = m;
            return m;
        }

        public static Material Particle(Color color)
        {
            EnsureShaders();
            string key = "ptc_" + ColorKey(color);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(particle);
            m.name = key;
            SetColor(m, color);
            cache[key] = m;
            return m;
        }

        static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static void SetFloat(Material m, string name, float v, string alt)
        {
            if (m.HasProperty(name)) m.SetFloat(name, v);
            if (!string.IsNullOrEmpty(alt) && m.HasProperty(alt)) m.SetFloat(alt, v);
        }

        static string ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 255) + "_" + Mathf.RoundToInt(c.g * 255) + "_" +
                   Mathf.RoundToInt(c.b * 255) + "_" + Mathf.RoundToInt(c.a * 255);
        }

        // -----------------------------------------------------------------
        //  Palet warna hangat khas warteg
        // -----------------------------------------------------------------
        public static class Palette
        {
            public static Color Floor = new Color(0.86f, 0.80f, 0.70f);
            public static Color FloorTile = new Color(0.78f, 0.71f, 0.61f);
            public static Color Wall = new Color(0.95f, 0.91f, 0.82f);
            public static Color WallAccent = new Color(0.85f, 0.42f, 0.28f);
            public static Color Wood = new Color(0.60f, 0.42f, 0.26f);
            public static Color WoodDark = new Color(0.42f, 0.29f, 0.18f);
            public static Color Metal = new Color(0.72f, 0.74f, 0.78f);
            public static Color DarkMetal = new Color(0.35f, 0.37f, 0.40f);
            public static Color Glass = new Color(0.80f, 0.90f, 0.95f);
            public static Color Skin = new Color(0.90f, 0.74f, 0.58f);
            public static Color SkinDark = new Color(0.76f, 0.58f, 0.42f);
            public static Color Hair = new Color(0.15f, 0.12f, 0.10f);
            public static Color Rice = new Color(0.98f, 0.96f, 0.90f);
            public static Color Stove = new Color(0.28f, 0.30f, 0.34f);
            public static Color Flame = new Color(1f, 0.55f, 0.12f);
            public static Color Leaf = new Color(0.30f, 0.58f, 0.25f);
            public static Color Awning = new Color(0.80f, 0.24f, 0.18f);
            public static Color Awning2 = new Color(0.97f, 0.94f, 0.86f);
            public static Color Road = new Color(0.30f, 0.31f, 0.34f);
            public static Color Grass = new Color(0.42f, 0.62f, 0.34f);
            public static Color Sky = new Color(0.62f, 0.78f, 0.92f);
            public static Color Night = new Color(0.10f, 0.12f, 0.22f);
            public static Color Money = new Color(0.30f, 0.80f, 0.35f);
            public static Color Danger = new Color(0.85f, 0.25f, 0.22f);
            public static Color Gold = new Color(1f, 0.82f, 0.30f);
        }
    }
}

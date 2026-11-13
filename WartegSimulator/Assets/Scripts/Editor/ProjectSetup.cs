using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Warteg.EditorTools
{
    /// <summary>
    /// Menyiapkan project secara otomatis saat pertama kali dibuka:
    /// scene utama, build settings, pipeline URP, dan pengaturan player.
    /// Semua bisa dijalankan ulang lewat menu "Warteg".
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string SettingsFolder = "Assets/Settings";
        const string UrpAssetPath = "Assets/Settings/WartegURPAsset.asset";
        const string UrpRendererPath = "Assets/Settings/WartegURPRenderer.asset";
        const string SetupDoneKey = "Warteg.ProjectSetupDone.v1";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            // jalankan sekali per sesi editor saja
            if (SessionState.GetBool(SetupDoneKey, false)) return;
            SessionState.SetBool(SetupDoneKey, true);

            try
            {
                ApplyPlayerSettings();
                EnsureFolders();
                EnsureScene();
                EnsureBuildSettings();
                EnsureUrp();
                EnsureAlwaysIncludedShaders();
                AssetDatabase.SaveAssets();
                Debug.Log("[Warteg] Setup project selesai. Tekan Play untuk memainkan Warteg Simulator.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Warteg] Setup otomatis gagal: " + e.Message + "\nGunakan menu Warteg > Jalankan Setup Project.");
            }
        }

        // =====================================================================
        [MenuItem("Warteg/Jalankan Setup Project", false, 1)]
        public static void ManualSetup()
        {
            SessionState.SetBool(SetupDoneKey, false);
            AutoSetup();
            EditorUtility.DisplayDialog("Warteg Simulator",
                "Setup selesai.\n\nScene: " + ScenePath + "\nBuka scene lalu tekan Play.", "OK");
        }

        /// <summary>
        /// Setup lengkap tanpa dialog — dipakai oleh build otomatis / CI.
        /// </summary>
        public static void SetupForBuild()
        {
            ApplyPlayerSettings();
            EnsureFolders();
            EnsureScene();
            EnsureBuildSettings();
            EnsureUrp();
            EnsureAlwaysIncludedShaders_Internal();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Warteg] SetupForBuild selesai.");
        }

        [MenuItem("Warteg/Buka Scene Main", false, 2)]
        public static void OpenMainScene()
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Warteg/Buat Ulang Scene Main (Repair)", false, 3)]
        public static void RebuildScene()
        {
            if (File.Exists(ScenePath)) AssetDatabase.DeleteAsset(ScenePath);
            CreateSceneAsset();
            EnsureBuildSettings();
            EditorUtility.DisplayDialog("Warteg Simulator", "Scene Main.unity dibuat ulang.", "OK");
        }

        // =====================================================================
        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(SettingsFolder)) AssetDatabase.CreateFolder("Assets", "Settings");
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath)) CreateSceneAsset();
        }

        static void CreateSceneAsset()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // kamera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.80f, 0.95f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.25f;
            cam.farClipPlane = 250f;
            camGo.transform.position = new Vector3(0f, 12f, 14f);
            camGo.transform.rotation = Quaternion.Euler(38f, 180f, 0f);
            camGo.AddComponent<AudioListener>();

            // cahaya matahari
            GameObject lightGo = new GameObject("Sun");
            Light sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, 155f, 0f);

            // bootstrap game
            GameObject bootGo = new GameObject("GameBootstrap");
            bootGo.AddComponent<GameBootstrap>();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[Warteg] Scene dibuat: " + ScenePath);
        }

        static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------------
        //  URP: buat pipeline asset bila belum ada (memakai reflection agar
        //  tidak gagal kompilasi ketika package URP belum terpasang).
        // ---------------------------------------------------------------------
        static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;

            Type assetType = FindType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
            Type rendererType = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
            if (assetType == null || rendererType == null)
            {
                Debug.LogWarning("[Warteg] Package URP tidak ditemukan. Game tetap berjalan dengan Built-in pipeline " +
                                 "(material otomatis memakai shader Standard). Instal 'Universal RP' lewat Package Manager " +
                                 "lalu jalankan menu Warteg > Jalankan Setup Project.");
                return;
            }

            try
            {
                ScriptableObject rendererData = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(rendererData, UrpRendererPath);

                MethodInfo create = null;
                MethodInfo[] methods = assetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != "Create") continue;
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(rendererType)) { create = methods[i]; break; }
                }

                UnityEngine.Object asset = null;
                if (create != null) asset = create.Invoke(null, new object[] { rendererData }) as UnityEngine.Object;
                else asset = ScriptableObject.CreateInstance(assetType);

                if (asset != null)
                {
                    AssetDatabase.CreateAsset(asset, UrpAssetPath);
                    PropertyInfo prop = typeof(GraphicsSettings).GetProperty("defaultRenderPipeline");
                    if (prop != null && prop.CanWrite) prop.SetValue(null, asset);
                    QualitySettings.renderPipeline = asset as RenderPipelineAsset;
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Warteg] URP pipeline asset dibuat: " + UrpAssetPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Warteg] Gagal membuat URP asset otomatis (" + e.Message +
                    "). Buat manual: Assets > Create > Rendering > URP Asset (with Universal Renderer), " +
                    "lalu pasang di Project Settings > Graphics.");
            }
        }

        // ---------------------------------------------------------------------
        //  Material dibuat 100% saat runtime lewat Shader.Find(). Tanpa
        //  "Always Included Shaders", Unity akan men-strip shader tersebut saat
        //  build sehingga semua objek tampil pink/tidak terlihat di APK & EXE.
        //  Method ini memastikan shader penting selalu ikut ter-build.
        // ---------------------------------------------------------------------
        public static readonly string[] RuntimeShaders =
        {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Standard",
            "Unlit/Color",
            "Sprites/Default",
            "Legacy Shaders/Diffuse",
            "Hidden/InternalErrorShader"
        };

        [MenuItem("Warteg/Perbaiki Shader untuk Build", false, 4)]
        public static void EnsureAlwaysIncludedShaders() { EnsureAlwaysIncludedShaders_Internal(); }

        public static void EnsureAlwaysIncludedShaders_Internal()
        {
            try
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (assets == null || assets.Length == 0) return;

                SerializedObject gs = new SerializedObject(assets[0]);
                SerializedProperty list = gs.FindProperty("m_AlwaysIncludedShaders");
                if (list == null) return;

                int added = 0;
                for (int i = 0; i < RuntimeShaders.Length; i++)
                {
                    Shader shader = Shader.Find(RuntimeShaders[i]);
                    if (shader == null) continue;
                    if (ContainsShader(list, shader)) continue;

                    list.InsertArrayElementAtIndex(list.arraySize);
                    SerializedProperty el = list.GetArrayElementAtIndex(list.arraySize - 1);
                    el.objectReferenceValue = shader;
                    added++;
                }

                if (added > 0)
                {
                    gs.ApplyModifiedProperties();
                    Debug.Log("[Warteg] " + added + " shader runtime ditambahkan ke Always Included Shaders.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Warteg] Gagal memperbarui Always Included Shaders otomatis (" + e.Message +
                    "). Buka Project Settings > Graphics > Always Included Shaders dan tambahkan " +
                    "'Universal Render Pipeline/Lit' secara manual bila objek tampak pink di build.");
            }
        }

        static bool ContainsShader(SerializedProperty list, Shader shader)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty el = list.GetArrayElementAtIndex(i);
                if (el != null && el.objectReferenceValue == shader) return true;
            }
            return false;
        }

        static Type FindType(string fullName)
        {
            Type t = Type.GetType(fullName);
            if (t != null) return t;
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                t = asms[i].GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        static void SetActiveInputHandler(string value)
        {
            try
            {
                Type t = FindType("UnityEditor.PlayerSettings");
                if (t == null) return;
                PropertyInfo p = t.GetProperty("activeInputHandler", BindingFlags.Public | BindingFlags.Static);
                if (p == null || !p.CanWrite) return;
                Type enumType = p.PropertyType;
                object parsed = Enum.Parse(enumType, value);
                p.SetValue(null, parsed);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Warteg] Tidak bisa set Active Input Handling otomatis (" + e.Message +
                    "). Set manual di Project Settings > Player > Active Input Handling = Both.");
            }
        }

        // ---------------------------------------------------------------------
        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Warteg Studio";
            PlayerSettings.productName = "Warteg Simulator";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.applicationIdentifier = "com.wartegstudio.wartegsimulator";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;

            // Input System (package com.unity.inputsystem) harus aktif, kalau tidak
            // InputService akan jatuh ke legacy input dan tidak merespons sentuhan.
            SetActiveInputHandler("Both");

            // Android: landscape + API 24 + ARM
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.wartegstudio.wartegsimulator");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            Debug.Log("[Warteg] Player settings diterapkan (1600x900, landscape, Android API 24).");
        }
    }
}

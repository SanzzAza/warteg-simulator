using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Warteg.EditorTools
{
    /// <summary>
    /// Build otomatis untuk Windows dan Android (APK/AAB).
    /// Bisa dipakai dari menu editor maupun lewat command line (CI):
    ///   Unity -quit -batchmode -projectPath . -executeMethod Warteg.EditorTools.BuildScript.BuildAndroidCLI
    /// </summary>
    public static class BuildScript
    {
        const string WindowsFolder = "Builds/Windows";
        const string AndroidFolder = "Builds/Android";

        // -----------------------------------------------------------------
        //  Menu editor
        // -----------------------------------------------------------------
        [MenuItem("Warteg/Build/Windows 64-bit", false, 20)]
        public static void BuildWindowsMenu() { BuildWindows(); OpenFolder(WindowsFolder); }

        [MenuItem("Warteg/Build/Android APK", false, 21)]
        public static void BuildAndroidMenu() { BuildAndroid(); OpenFolder(AndroidFolder); }

        [MenuItem("Warteg/Build/Android AAB (Play Store)", false, 22)]
        public static void BuildAndroidAabMenu() { BuildAndroidBundle(); OpenFolder(AndroidFolder); }

        [MenuItem("Warteg/Build/Build Semua (Windows + APK)", false, 23)]
        public static void BuildAllMenu()
        {
            BuildWindows();
            BuildAndroid();
            OpenFolder("Builds");
        }

        // -----------------------------------------------------------------
        //  Entry point CLI (dipakai Tools/build.sh)
        // -----------------------------------------------------------------
        public static void BuildWindowsCLI() { bool ok = BuildWindows(); EditorApplication.Exit(ok ? 0 : 1); }
        public static void BuildAndroidCLI() { bool ok = BuildAndroid(); EditorApplication.Exit(ok ? 0 : 1); }
        public static void BuildAndroidAabCLI() { bool ok = BuildAndroidBundle(); EditorApplication.Exit(ok ? 0 : 1); }

        // -----------------------------------------------------------------
        //  Windows
        // -----------------------------------------------------------------
        public static bool BuildWindows()
        {
            if (!CheckSceneReady()) return false;

            string dir = Path.Combine(ProjectRoot, WindowsFolder);
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "WartegSimulator.exe");

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.buildAppBundle = false;
            return RunBuild(BuildTarget.StandaloneWindows64, exe, false);
        }

        // -----------------------------------------------------------------
        //  Android APK
        // -----------------------------------------------------------------
        public static bool BuildAndroid()
        {
            if (!CheckSceneReady()) return false;
            if (!CheckAndroidModule()) return false;

            ApplyAndroidSettings();

            string dir = Path.Combine(ProjectRoot, AndroidFolder);
            Directory.CreateDirectory(dir);
            string apk = Path.Combine(dir, "WartegSimulator.apk");

            EditorUserBuildSettings.buildAppBundle = false;

            // Coba IL2CPP + ARM64 (standar Play Store). IL2CPP butuh NDK;
            // kalau modul NDK belum terpasang, otomatis mundur ke Mono + ARMv7
            // supaya APK tetap bisa dibangun untuk uji coba di HP.
            if (TrySetBackend(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64))
            {
                Debug.Log("[Warteg] Android: mencoba IL2CPP + ARM64...");
                if (RunBuild(BuildTarget.Android, apk, false)) return true;
                Debug.LogWarning("[Warteg] Build IL2CPP gagal. Mencoba ulang dengan Mono + ARMv7 " +
                                 "(butuh Android Build Support tanpa NDK).");
            }
            else
            {
                Debug.LogWarning("[Warteg] IL2CPP/ARM64 tidak tersedia. Memakai Mono + ARMv7.");
            }

            TrySetBackend(ScriptingImplementation.Mono2x, AndroidArchitecture.ARMv7);
            return RunBuild(BuildTarget.Android, apk, false);
        }

        // -----------------------------------------------------------------
        //  Android App Bundle (AAB)
        // -----------------------------------------------------------------
        public static bool BuildAndroidBundle()
        {
            if (!CheckSceneReady()) return false;
            if (!CheckAndroidModule()) return false;

            ApplyAndroidSettings();

            string dir = Path.Combine(ProjectRoot, AndroidFolder);
            Directory.CreateDirectory(dir);
            string aab = Path.Combine(dir, "WartegSimulator.aab");

            EditorUserBuildSettings.buildAppBundle = true;
            bool ok;
            if (TrySetBackend(ScriptingImplementation.IL2CPP, AndroidArchitecture.ARM64))
                ok = RunBuild(BuildTarget.Android, aab, true);
            else
            {
                Debug.LogWarning("[Warteg] AAB Play Store wajib IL2CPP/ARM64. Pasang modul IL2CPP + NDK di Unity Hub.");
                ok = false;
            }
            EditorUserBuildSettings.buildAppBundle = false;
            return ok;
        }

        // -----------------------------------------------------------------
        //  Helper
        // -----------------------------------------------------------------
        static string ProjectRoot
        {
            get
            {
                // Application.dataPath = <project>/Assets
                string p = Application.dataPath;
                if (!string.IsNullOrEmpty(p) && p.EndsWith("/Assets", StringComparison.Ordinal))
                    return p.Substring(0, p.Length - "/Assets".Length);
                return Directory.GetCurrentDirectory();
            }
        }

        static bool CheckSceneReady()
        {
            // pastikan shader runtime ikut ter-build (kalau tidak, objek jadi pink)
            ProjectSetup.EnsureAlwaysIncludedShaders_Internal();

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Warteg] Tidak ada scene di Build Settings. Jalankan menu 'Warteg > Jalankan Setup Project'.");
                return false;
            }
            return true;
        }

        static bool CheckAndroidModule()
        {
            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) return true;
            Debug.LogError("[Warteg] Modul 'Android Build Support' belum terpasang. " +
                           "Buka Unity Hub > Installs > gear icon > Add modules > centang Android Build Support " +
                           "(+ SDK/NDK/JDK), lalu build ulang.");
            return false;
        }

        static void ApplyAndroidSettings()
        {
            PlayerSettings.companyName = "Warteg Studio";
            PlayerSettings.productName = "Warteg Simulator";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.wartegstudio.wartegsimulator");
            PlayerSettings.bundleVersion = "1.0.0";

            // Android 7.0 (API 24) sesuai syarat minimum Input System/URP 14
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.blitType = AndroidBlitType.Always;

            // layar sentuh landscape
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        static bool TrySetBackend(ScriptingImplementation impl, AndroidArchitecture arch)
        {
            try
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, impl);
                PlayerSettings.Android.targetArchitectures = arch;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Warteg] Tidak bisa set backend " + impl + ": " + e.Message);
                return false;
            }
        }

        static bool RunBuild(BuildTarget target, string path, bool appBundle)
        {
            string[] scenes = EnabledScenes();

            BuildPlayerOptions opts = new BuildPlayerOptions();
            opts.scenes = scenes;
            opts.locationPathName = path;
            opts.target = target;
            opts.options = BuildOptions.None;

            Debug.Log("[Warteg] Mulai build " + target + (appBundle ? " (AAB)" : "") + " → " + path);
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report == null)
            {
                Debug.LogError("[Warteg] Build gagal: BuildReport kosong (cek Console untuk error kompilasi).");
                return false;
            }

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Warteg] Build SUKSES: " + path +
                          " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                return true;
            }

            Debug.LogError("[Warteg] Build GAGAL: " + report.summary.result +
                           " — total error: " + report.summary.totalErrors);
            return false;
        }

        static string[] EnabledScenes()
        {
            EditorBuildSettingsScene[] all = EditorBuildSettings.scenes;
            List<string> list = new List<string>();
            if (all != null)
                for (int i = 0; i < all.Length; i++)
                    if (all[i].enabled) list.Add(all[i].path);

            if (list.Count == 0 && File.Exists(ProjectSetup.ScenePath)) list.Add(ProjectSetup.ScenePath);
            return list.ToArray();
        }

        static void OpenFolder(string relative)
        {
            try
            {
                string full = Path.Combine(ProjectRoot, relative);
                if (Directory.Exists(full)) EditorUtility.RevealInFinder(full);
            }
            catch { /* abaikan */ }
        }
    }
}

using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Titik masuk game. Scene hanya berisi satu objek dengan komponen ini;
    /// seluruh dunia 3D, sistem, dan UI dibangun dari sini secara prosedural.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public bool buildOnAwake = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // menjaga agar domain reload / play ulang di editor tetap bersih
        }

        void Awake()
        {
            if (buildOnAwake) BuildGame();
        }

        public void BuildGame()
        {
            ApplyPlatformSettings();
            Catalog.EnsureLoaded();

            Camera cam = SetupCamera();
            Light sun = SetupSun();
            GameManager gm = SetupGameManager();

            // ---- audio ----
            AudioManager audio = gm.gameObject.AddComponent<AudioManager>();
            audio.Init();
            gm.Audio = audio;
            audio.ApplySettings(SaveSystem.LoadSettings());

            // ---- dunia 3D ----
            GameObject worldGo = new GameObject("World");
            WorldBuilder world = worldGo.AddComponent<WorldBuilder>();
            world.BuildWorld(cam, sun);
            gm.World = world;

            GameObject kitchenGo = new GameObject("Kitchen");
            kitchenGo.transform.SetParent(worldGo.transform, false);
            KitchenManager kitchen = kitchenGo.AddComponent<KitchenManager>();
            kitchen.root = kitchenGo.transform;
            gm.Kitchen = kitchen;

            // ---- gameplay systems ----
            GameObject systems = new GameObject("Systems");
            UpgradeSystem upgrades = systems.AddComponent<UpgradeSystem>();
            gm.Upgrades = upgrades;

            CustomerSpawner spawner = systems.AddComponent<CustomerSpawner>();
            spawner.Init();
            gm.Spawner = spawner;

            StaffManager staff = systems.AddComponent<StaffManager>();
            staff.Init();
            gm.StaffMgr = staff;

            JuiceFX fx = systems.AddComponent<JuiceFX>();
            fx.Init(cam);
            gm.Flow = GameFlow.MainMenu;

            // ---- pemain & kamera ----
            GameObject playerGo = new GameObject("Player");
            PlayerController player = playerGo.AddComponent<PlayerController>();
            player.Init(cam, world.PlayerSpawnPoint);
            player.moveBoundsCenter = new Vector3(0f, 0f, world.DoorOutside.z * 0.45f);
            player.moveBounds = new Vector2(9f, 11.5f);
            gm.Player = player;

            CameraRig cameraRig = cam.gameObject.AddComponent<CameraRig>();
            cameraRig.Init(cam, playerGo.transform);
            cameraRig.yaw = 180f;
            cameraRig.pitch = 50f;
            cameraRig.distance = 16f;

            // ---- UI ----
            UIFactory.EnsureEventSystem();

            GameObject uiGo = new GameObject("UI");
            HUDUI hud = uiGo.AddComponent<HUDUI>();
            hud.Build();
            gm.Hud = hud;

            MainMenuUI mainMenu = uiGo.AddComponent<MainMenuUI>();
            mainMenu.Build();
            gm.MainMenu = mainMenu;

            ManagementUI management = uiGo.AddComponent<ManagementUI>();
            management.Build();
            gm.Management = management;

            SettingsUI settings = uiGo.AddComponent<SettingsUI>();
            settings.Build();
            gm.Settings = settings;

            DayReportUI dayReport = uiGo.AddComponent<DayReportUI>();
            dayReport.Build();
            gm.DayReport = dayReport;

            // ---- state awal untuk latar menu ----
            gm.Data = new SaveData();
            gm.Data.settings = SaveSystem.LoadSettings();
            gm.Display = world.Etalase;
            gm.ApplyData();

            hud.SetVisible(false);
            mainMenu.SetVisible(true);
            mainMenu.RefreshContinueButton();

            Debug.Log("[Warteg] Game siap. persistentDataPath = " + Application.persistentDataPath);
        }

        /// <summary>
        /// Pengaturan khusus perangkat: target FPS & layar tidak mati saat bermain
        /// (penting untuk Android, tidak berpengaruh di Windows).
        /// </summary>
        static void ApplyPlatformSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // ---------------------------------------------------------------------
        static Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.80f, 0.95f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.25f;
            cam.farClipPlane = 250f;
            cam.transform.position = new Vector3(0f, 12f, 14f);
            cam.transform.rotation = Quaternion.Euler(35f, 180f, 0f);

            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }

        static Light SetupSun()
        {
            Light[] lights = Object.FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional) return lights[i];

            GameObject go = new GameObject("Sun");
            Light sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, 155f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);
            return sun;
        }

        static GameManager SetupGameManager()
        {
            GameManager existing = GameManager.I;
            if (existing != null) return existing;

            GameObject go = new GameObject("GameManager");
            GameManager gm = go.AddComponent<GameManager>();
            return gm;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    public enum GameFlow { MainMenu = 0, Playing = 1 }

    /// <summary>
    /// Pusat permainan: menyimpan state, jam, uang, progres, dan menghubungkan semua sistem.
    /// Dibuat otomatis oleh GameBootstrap saat scene dimulai.
    /// </summary>
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public SaveData Data;
        public TimeSystem Time;
        public GameFlow Flow = GameFlow.MainMenu;

        // sistem runtime (diisi oleh GameBootstrap)
        [NonSerialized] public WorldBuilder World;
        [NonSerialized] public KitchenManager Kitchen;
        [NonSerialized] public DisplayCase Display;
        [NonSerialized] public CustomerSpawner Spawner;
        [NonSerialized] public StaffManager StaffMgr;
        [NonSerialized] public UpgradeSystem Upgrades;
        [NonSerialized] public HUDUI Hud;
        [NonSerialized] public PlayerController Player;
        [NonSerialized] public AudioManager Audio;
        [NonSerialized] public MainMenuUI MainMenu;
        [NonSerialized] public ManagementUI Management;
        [NonSerialized] public DayReportUI DayReport;
        [NonSerialized] public SettingsUI Settings;

        // ---------------- events ----------------
        public event Action OnMoneyChanged;
        public event Action OnInventoryChanged;
        public event Action OnDisplayChanged;
        public event Action OnProgressChanged;      // level / reputasi / xp
        public event Action OnClockChanged;         // jam / hari / cuaca
        public event Action<int> OnDayRolled;       // hari baru dimulai
        public event Action OnStateReloaded;        // setelah new game / load
        public event Action<string, int> OnNotify;  // toast (pesan, jenis: 0=info 1=sukses 2=gagal)

        int pauseDepth;
        float autosaveTimer;
        float lastHourFloat;
        bool endOfDayHandled;
        bool dirtySave;

        public bool IsPaused { get { return pauseDepth > 0 || Flow != GameFlow.Playing; } }
        public bool IsPlaying { get { return Data != null && Flow == GameFlow.Playing; } }

        // =====================================================================
        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (!IsPlaying) return;

            float dt = UnityEngine.Time.deltaTime;

            if (!IsPaused)
            {
                Time.Advance(dt, Data.settings.timeScale);

                // pembusukan makanan mengikuti jam game
                float dh = Time.Hour - lastHourFloat;
                if (dh > 0f && Display != null) Display.TickSpoilage(dh);
                lastHourFloat = Time.Hour;

                Data.totalPlaySeconds += Mathf.RoundToInt(dt);
                TickCleanliness(dt);

                autosaveTimer += dt;
                if (autosaveTimer >= 60f) { autosaveTimer = 0f; Autosave(); }

                if (Time.Hour >= Catalog.CloseHour && !endOfDayHandled)
                {
                    endOfDayHandled = true;
                    EndOfDay();
                }
            }

            if (InputService.MenuPressed()) TogglePauseMenu();
        }

        public void PushPause() { pauseDepth++; }
        public void PopPause() { pauseDepth = Mathf.Max(0, pauseDepth - 1); }

        void TogglePauseMenu()
        {
            if (ToastOverlay_IsBlocking()) return;
            if (Management != null && Management.IsOpen) { Management.Close(); return; }
            if (Settings != null && Settings.IsOpen) { Settings.Close(); return; }
            if (DayReport != null && DayReport.IsOpen) return;
            if (Hud != null && Hud.IsPausedMenuOpen) { Hud.TogglePauseMenu(); return; }
            if (Hud != null) Hud.TogglePauseMenu();
        }

        bool ToastOverlay_IsBlocking()
        {
            return false;
        }

        // =====================================================================
        //  MULAI / MUAT
        // =====================================================================
        public void StartNewGame()
        {
            Data = CreateNewSave();
            ApplyData();
            Flow = GameFlow.Playing;
            if (Hud != null) Hud.SetVisible(true);
            if (MainMenu != null) MainMenu.SetVisible(false);
            Notify("Selamat datang di " + Data.wartegName + "! Modal awal " + Catalog.Money(Data.money), 1);
        }

        public bool ContinueGame()
        {
            SaveData d = SaveSystem.Load();
            if (d == null) return false;
            Data = d;
            ApplyData();
            Flow = GameFlow.Playing;
            if (Hud != null) Hud.SetVisible(true);
            if (MainMenu != null) MainMenu.SetVisible(false);
            Notify("Progres dimuat — Hari " + Data.day, 1);
            return true;
        }

        SaveData CreateNewSave()
        {
            SaveData d = new SaveData();
            d.money = Catalog.StartMoney;
            d.day = 1;
            d.hour = Catalog.OpenHour;
            d.reputation = 12;
            d.level = 1;
            d.xp = 0f;
            d.cleanliness = 100f;

            // stok awal sederhana supaya pemain bisa langsung berjualan
            AddStack(d.storage, "beras", 2);
            AddStack(d.storage, "ayam", 1);
            AddStack(d.storage, "telur", 10);
            AddStack(d.storage, "tempe", 5);
            AddStack(d.storage, "tahu", 5);
            AddStack(d.storage, "minyak", 10);
            AddStack(d.storage, "bumbu", 4);
            AddStack(d.storage, "sayurAsem", 1);
            AddStack(d.storage, "kangkung", 1);
            AddStack(d.storage, "teh", 10);
            AddStack(d.storage, "gula", 10);
            AddStack(d.storage, "es", 10);
            AddStack(d.storage, "air", 6);
            AddStack(d.storage, "gas", 40);

            // semua menu terdaftar; hanya yang level-nya cukup terbuka
            d.menu.Clear();
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                MenuEntrySave e = new MenuEntrySave();
                e.id = r.id;
                e.price = r.defaultPrice;
                e.unlocked = r.unlockLevel <= 1;
                d.menu.Add(e);
            }

            d.settings = SaveSystem.LoadSettings();
            return d;
        }

        static void AddStack(List<IngredientStack> list, string id, int qty)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id) { list[i].qty += qty; return; }
            }
            list.Add(new IngredientStack(id, qty));
        }

        public void ApplyData()
        {
            Catalog.EnsureLoaded();
            EnsureMenuIntegrity();

            Time = new TimeSystem(Data.day, Data.hour, (Weather)UnityEngine.Random.Range(0, 3));
            Time.OnClockHook = OnClockChangedInternal;
            Time.OnDayStarted += OnNewDayStarted;
            lastHourFloat = Time.Hour;
            endOfDayHandled = false;
            autosaveTimer = 0f;
            pauseDepth = 0;

            if (World != null) World.Rebuild(Data);
            if (Kitchen != null) Kitchen.Rebuild(Data);
            if (Display != null) Display.Rebuild(Data);
            if (StaffMgr != null) StaffMgr.Rebuild(Data);
            if (Upgrades != null) Upgrades.Rebuild(Data);
            if (Spawner != null) Spawner.ResetSpawner();
            if (Hud != null) Hud.RefreshAll();
            if (Audio != null) { Audio.ApplySettings(Data.settings); Audio.PlayAmbient(Time.Weather); }
            if (OnStateReloaded != null) OnStateReloaded();
        }

        void EnsureMenuIntegrity()
        {
            if (Data.menu == null) Data.menu = new List<MenuEntrySave>();
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                MenuEntrySave found = null;
                for (int j = 0; j < Data.menu.Count; j++) if (Data.menu[j].id == r.id) { found = Data.menu[j]; break; }
                if (found == null)
                {
                    found = new MenuEntrySave();
                    found.id = r.id;
                    found.price = r.defaultPrice;
                    found.unlocked = r.unlockLevel <= Data.level;
                    Data.menu.Add(found);
                }
                if (found.price <= 0) found.price = r.defaultPrice;
                if (r.unlockLevel <= Data.level) found.unlocked = true;
            }
        }

        void OnClockChangedInternal()
        {
            if (OnClockChanged != null) OnClockChanged();
            if (Data != null) Data.hour = Time.Hour;
        }

        void OnNewDayStarted(int day)
        {
            Data.day = day;
            Data.hour = Time.Hour;
            endOfDayHandled = false;
            if (World != null) World.SetWeatherVisual(Time.Weather);
            if (Audio != null) Audio.PlayAmbient(Time.Weather);
            if (Spawner != null) Spawner.ResetSpawner();
            if (Hud != null) Hud.RefreshAll();
            if (OnDayRolled != null) OnDayRolled(day);
            SaveGame(true);
        }

        // =====================================================================
        //  SAVE
        // =====================================================================
        public void SaveGame(bool silent)
        {
            if (Data == null) return;
            Data.day = Time.Day;
            Data.hour = Time.Hour;
            bool ok = SaveSystem.Save(Data);
            dirtySave = false;
            if (!silent)
                Notify(ok ? "Progres disimpan ✔" : "Gagal menyimpan!", ok ? 1 : 2);
            if (ok && silent) Debug.Log("[Warteg] Autosave ok (Hari " + Data.day + ")");
        }

        void Autosave() { SaveGame(true); }

        public void SaveSettings()
        {
            if (Data != null) SaveSystem.SaveSettings(Data.settings);
        }

        public void QuitToMainMenu(bool save)
        {
            if (save) SaveGame(true);
            Flow = GameFlow.MainMenu;
            if (Hud != null) Hud.SetVisible(false);
            if (Management != null) Management.Close();
            if (Spawner != null) Spawner.ClearAllCustomers();
            if (MainMenu != null) { MainMenu.SetVisible(true); MainMenu.RefreshContinueButton(); }
        }

        public void QuitApplication()
        {
            SaveGame(true);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =====================================================================
        //  UTIL
        // =====================================================================
        public void NotifyDisplayChanged()
        {
            if (OnDisplayChanged != null) OnDisplayChanged();
        }

        public void NotifyInventoryChanged()
        {
            if (OnInventoryChanged != null) OnInventoryChanged();
        }

        public void Notify(string message, int kind)
        {
            if (OnNotify != null) OnNotify(message, kind);
            else Debug.Log("[Warteg] " + message);
        }

        public void Notify(string message) { Notify(message, 0); }

        public RecipeDef Recipe(string id) { return Catalog.Recipe(id); }

        public MenuEntrySave MenuEntry(string id)
        {
            for (int i = 0; i < Data.menu.Count; i++) if (Data.menu[i].id == id) return Data.menu[i];
            return null;
        }

        public int PriceOf(string recipeId)
        {
            MenuEntrySave e = MenuEntry(recipeId);
            if (e != null) return e.price;
            RecipeDef r = Catalog.Recipe(recipeId);
            return r != null ? r.defaultPrice : 0;
        }

        public bool IsUnlocked(string recipeId)
        {
            MenuEntrySave e = MenuEntry(recipeId);
            return e != null && e.unlocked;
        }

        // =====================================================================
        //  KEBERSIHAN
        // =====================================================================
        void TickCleanliness(float dt)
        {
            // turun perlahan seiring waktu, lebih cepat saat banyak pelanggan
            float decayPerSecond = 0.06f * (Upgrades != null ? Upgrades.CleanDecayMul : 1f);
            Data.cleanliness = Mathf.Clamp(Data.cleanliness - decayPerSecond * dt, 0f, 100f);
        }

        public void AddDirt(float amount)
        {
            float mul = Upgrades != null ? Upgrades.CleanDecayMul : 1f;
            Data.cleanliness = Mathf.Clamp(Data.cleanliness - amount * mul, 0f, 100f);
            if (Hud != null) Hud.RefreshStatus();
        }

        public void CleanWarteg(float amount)
        {
            Data.cleanliness = Mathf.Clamp(Data.cleanliness + amount, 0f, 100f);
            if (Hud != null) Hud.RefreshStatus();
        }

        public float Cleanliness { get { return Data.cleanliness; } }
    }
}

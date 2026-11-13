using System;
using System.IO;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Penyimpanan lokal (JSON) memakai Application.persistentDataPath.
    /// Dipakai untuk progres permainan dan pengaturan audio.
    /// </summary>
    public static class SaveSystem
    {
        const string SaveFile = "warteg_save.json";
        const string BackupFile = "warteg_save_backup.json";
        const string SettingsFile = "warteg_settings.json";

        public static string SavePath { get { return Path.Combine(Application.persistentDataPath, SaveFile); } }
        public static string SettingsPath { get { return Path.Combine(Application.persistentDataPath, SettingsFile); } }

        public static bool HasSave
        {
            get
            {
                try { return File.Exists(SavePath); }
                catch { return false; }
            }
        }

        public static bool Save(SaveData data)
        {
            if (data == null) return false;
            try
            {
                string json = JsonUtility.ToJson(data, true);
                // tulis ke file sementara dulu supaya save lama tidak rusak jika crash
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupFile, true);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Gagal menyimpan: " + e.Message);
                return false;
            }
        }

        public static SaveData Load()
        {
            SaveData data = LoadFrom(SavePath);
            if (data == null) data = LoadFrom(BackupFile);   // fallback ke backup
            if (data == null) return null;
            Repair(data);
            return data;
        }

        static SaveData LoadFrom(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] Gagal memuat " + path + ": " + e.Message);
                return null;
            }
        }

        /// <summary>Memastikan data lama / tidak lengkap tetap bisa dimainkan.</summary>
        static void Repair(SaveData d)
        {
            if (d.storage == null) d.storage = new System.Collections.Generic.List<IngredientStack>();
            if (d.menu == null) d.menu = new System.Collections.Generic.List<MenuEntrySave>();
            if (d.display == null) d.display = new System.Collections.Generic.List<DisplaySlotSave>();
            if (d.staff == null) d.staff = new System.Collections.Generic.List<StaffSave>();
            if (d.upgrades == null) d.upgrades = new System.Collections.Generic.List<string>();
            if (d.ledger == null) d.ledger = new System.Collections.Generic.List<LedgerEntry>();
            if (d.soldToday == null) d.soldToday = new System.Collections.Generic.List<IngredientStack>();
            if (d.history == null) d.history = new System.Collections.Generic.List<DailyReport>();
            if (d.stats == null) d.stats = new StatSave();
            if (d.stats.soldPerItem == null) d.stats.soldPerItem = new System.Collections.Generic.List<IngredientStack>();
            if (d.stats.cookedPerItem == null) d.stats.cookedPerItem = new System.Collections.Generic.List<IngredientStack>();
            if (d.settings == null) d.settings = new SettingsSave();
            if (d.level < 1) d.level = 1;
            if (d.day < 1) d.day = 1;
            if (d.hour < Catalog.OpenHour) d.hour = Catalog.OpenHour;
            d.cleanliness = Mathf.Clamp(d.cleanliness, 0f, 100f);
            d.reputation = Mathf.Clamp(d.reputation, 0, 100);
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupFile)) File.Delete(BackupFile);
            }
            catch (Exception e) { Debug.LogWarning("[SaveSystem] Gagal menghapus save: " + e.Message); }
        }

        public static void SaveSettings(SettingsSave s)
        {
            try { File.WriteAllText(SettingsPath, JsonUtility.ToJson(s, true)); }
            catch (Exception e) { Debug.LogWarning("[SaveSystem] Gagal menyimpan setting: " + e.Message); }
        }

        public static SettingsSave LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    SettingsSave s = JsonUtility.FromJson<SettingsSave>(File.ReadAllText(SettingsPath));
                    if (s != null) return s;
                }
            }
            catch (Exception e) { Debug.LogWarning("[SaveSystem] Gagal memuat setting: " + e.Message); }
            return new SettingsSave();
        }

        public static string SaveInfo()
        {
            SaveData d = Load();
            if (d == null) return "Belum ada progres tersimpan";
            return "Hari " + d.day + " • " + Catalog.Money(d.money) + " • " + Catalog.LevelTitle(d.level) + " (Lv " + d.level + ")";
        }
    }
}

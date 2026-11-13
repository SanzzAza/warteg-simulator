using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    // =====================================================================
    //  ENUM DASAR
    // =====================================================================

    public enum Weather { Cerah = 0, Mendung = 1, Hujan = 2 }

    public enum DayPhase { Pagi = 0, Siang = 1, Sore = 2, Malam = 3 }

    public enum FoodCategory { Makanan = 0, Minuman = 1 }

    public enum CustomerKind { Karyawan = 0, AnakSekolah = 1, Mahasiswa = 2, Ojol = 3, Warga = 4, Keluarga = 5 }

    public enum StaffRole { Koki = 0, Kasir = 1, Pelayan = 2, Kebersihan = 3 }

    public enum LedgerKind { Pemasukan = 0, Pengeluaran = 1 }

    public enum LedgerCategory
    {
        Penjualan = 0, Belanja = 1, Gaji = 2, Listrik = 3, Air = 4,
        Sewa = 5, Perawatan = 6, Upgrade = 7, Lain = 8
    }

    // =====================================================================
    //  DATA YANG DISIMPAN (JsonUtility friendly: field publik, tanpa Dictionary)
    // =====================================================================

    [Serializable]
    public class IngredientStack
    {
        public string id;
        public int qty;
        public IngredientStack() { }
        public IngredientStack(string id, int qty) { this.id = id; this.qty = qty; }
    }

    [Serializable]
    public class MenuEntrySave
    {
        public string id;
        public int price;
        public bool unlocked;
    }

    /// <summary>Satu tumpukan makanan siap jual di etalase.</summary>
    [Serializable]
    public class DisplaySlotSave
    {
        public string foodId;
        public int portions;
        public float freshness;          // 0..100
        public float cookHour;           // jam game saat dimasak (untuk info)
    }

    [Serializable]
    public class StaffSave
    {
        public string id;
        public string name;
        public int role;                 // StaffRole
        public int salary;               // per hari
        public float speed;              // 0.6 - 1.6
        public float skill;              // 0 - 100
        public int level;
        public int hiredDay;
    }

    [Serializable]
    public class LedgerEntry
    {
        public int day;
        public float hour;
        public int kind;                 // LedgerKind
        public int category;             // LedgerCategory
        public int amount;               // selalu positif
        public string note;
    }

    [Serializable]
    public class DailyReport
    {
        public int day;
        public int revenue;
        public int expense;
        public int customersServed;
        public int customersLost;
        public float avgSatisfaction;
        public int weather;
        public string bestSeller;
        public int profit;
    }

    [Serializable]
    public class StatSave
    {
        public int totalRevenue;
        public int totalExpense;
        public int totalCustomers;
        public int totalLost;
        public int totalSpoiled;
        public int daysPlayed;
        public int bestDayProfit;
        public List<IngredientStack> soldPerItem = new List<IngredientStack>();
        public List<IngredientStack> cookedPerItem = new List<IngredientStack>();
    }

    [Serializable]
    public class SettingsSave
    {
        public float musicVolume = 0.45f;
        public float sfxVolume = 0.75f;
        public float timeScale = 1f;         // pengali kecepatan waktu
        public bool showTutorialHints = true;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string wartegName = "Warteg Berkah";
        public int money = 2000000;
        public int day = 1;
        public float hour = 6f;              // 6.0 .. 23.0
        public int reputation = 12;          // 0..100
        public float xp = 0f;
        public int level = 1;
        public float cleanliness = 100f;

        public List<IngredientStack> storage = new List<IngredientStack>();
        public List<MenuEntrySave> menu = new List<MenuEntrySave>();
        public List<DisplaySlotSave> display = new List<DisplaySlotSave>();
        public List<StaffSave> staff = new List<StaffSave>();
        public List<string> upgrades = new List<string>();
        public List<LedgerEntry> ledger = new List<LedgerEntry>();
        public List<DailyReport> history = new List<DailyReport>();
        public StatSave stats = new StatSave();
        public SettingsSave settings = new SettingsSave();

        public int dayRevenue;               // akumulasi hari berjalan
        public int dayExpense;
        public int dayCustomersServed;
        public int dayCustomersLost;
        public float daySatisfactionSum;
        public int daySatisfactionCount;
        public List<IngredientStack> soldToday = new List<IngredientStack>();
        public int totalPlaySeconds;
    }
}

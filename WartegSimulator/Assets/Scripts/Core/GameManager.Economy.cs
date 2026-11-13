using System;
using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Bagian keuangan: pemasukan, pengeluaran, tagihan harian, laporan.</summary>
    public partial class GameManager
    {
        public int Money { get { return Data != null ? Data.money : 0; } }
        public bool CanAfford(int amount) { return Data != null && Data.money >= amount; }

        public void AddMoney(int amount, LedgerCategory cat, string note)
        {
            if (amount <= 0) return;
            Data.money += amount;
            Data.dayRevenue += amount;
            Data.stats.totalRevenue += amount;
            LogLedger(LedgerKind.Pemasukan, cat, amount, note);
            if (OnMoneyChanged != null) OnMoneyChanged();
            if (Hud != null) Hud.RefreshMoney();
        }

        public bool SpendMoney(int amount, LedgerCategory cat, string note)
        {
            if (amount <= 0) return true;
            if (Data.money < amount)
            {
                Notify("Uang tidak cukup! Butuh " + Catalog.Money(amount - Data.money) + " lagi.", 2);
                return false;
            }
            Data.money -= amount;
            Data.dayExpense += amount;
            Data.stats.totalExpense += amount;
            LogLedger(LedgerKind.Pengeluaran, cat, amount, note);
            if (OnMoneyChanged != null) OnMoneyChanged();
            if (Hud != null) Hud.RefreshMoney();
            return true;
        }

        /// <summary>Pengeluaran yang boleh membuat saldo minus (mis. tagihan wajib).</summary>
        void ForceExpense(int amount, LedgerCategory cat, string note)
        {
            Data.money -= amount;
            Data.dayExpense += amount;
            Data.stats.totalExpense += amount;
            LogLedger(LedgerKind.Pengeluaran, cat, amount, note);
            if (OnMoneyChanged != null) OnMoneyChanged();
            if (Hud != null) Hud.RefreshMoney();
        }

        /// <summary>Pengeluaran paksa yang bisa dipanggil sistem lain (mis. pesangon).</summary>
        public void ForceExpensePublic(int amount, LedgerCategory cat, string note)
        {
            ForceExpense(amount, cat, note);
        }

        void LogLedger(LedgerKind kind, LedgerCategory cat, int amount, string note)
        {
            LedgerEntry e = new LedgerEntry();
            e.day = Time != null ? Time.Day : Data.day;
            e.hour = Time != null ? Time.Hour : Data.hour;
            e.kind = (int)kind;
            e.category = (int)cat;
            e.amount = amount;
            e.note = note;
            Data.ledger.Add(e);
            // jaga ukuran file save
            if (Data.ledger.Count > 300) Data.ledger.RemoveRange(0, Data.ledger.Count - 300);
        }

        public List<LedgerEntry> TodayLedger()
        {
            List<LedgerEntry> list = new List<LedgerEntry>();
            int today = Time != null ? Time.Day : Data.day;
            for (int i = Data.ledger.Count - 1; i >= 0; i--)
                if (Data.ledger[i].day == today) list.Add(Data.ledger[i]);
            return list;
        }

        public int DayProfit { get { return Data.dayRevenue - Data.dayExpense; } }

        // =====================================================================
        //  TAGIHAN HARIAN
        // =====================================================================
        public int ElectricityCost()
        {
            int stoves = Upgrades != null ? Upgrades.StoveCount : Catalog.BaseStoves;
            int baseCost = 6000 + stoves * 1400 + Data.dayCustomersServed * 320;
            if (Upgrades != null && Upgrades.Has("lampu")) baseCost += 1500;
            if (Upgrades != null && Upgrades.Has("kulkas")) baseCost += 3500;
            if (Upgrades != null && Upgrades.Has("dapur_modern")) baseCost += 4000;
            return baseCost;
        }

        public int WaterCost()
        {
            int baseCost = 3000 + Data.dayCustomersServed * 210 + (Upgrades != null ? Upgrades.StoveCount * 300 : 0);
            return baseCost;
        }

        public int StaffSalaryTotal()
        {
            int total = 0;
            for (int i = 0; i < Data.staff.Count; i++) total += Data.staff[i].salary;
            return total;
        }

        public int MaintenanceCost()
        {
            int equip = (Upgrades != null ? Upgrades.StoveCount : 1) + (Upgrades != null ? Upgrades.TableCount : 1);
            int total = equip * 1800 + Data.staff.Count * 600;
            if (Upgrades != null) total += Upgrades.OwnedCount * 400;
            return total;
        }

        public bool RentDueToday()
        {
            int day = Time != null ? Time.Day : Data.day;
            return day > 1 && day % 7 == 0;
        }

        public int RentAmount { get { return Catalog.RentPerWeek; } }

        // =====================================================================
        //  AKHIR HARI
        // =====================================================================
        void EndOfDay()
        {
            if (Spawner != null) Spawner.StopSpawning();

            int electricity = ElectricityCost();
            int water = WaterCost();
            int salary = StaffSalaryTotal();
            int maintenance = MaintenanceCost();

            // tagihan wajib — boleh membuat saldo minus (pemain bangkrut / harus pinjam)
            ForceExpense(electricity, LedgerCategory.Listrik, "Tagihan listrik harian");
            ForceExpense(water, LedgerCategory.Air, "Tagihan air harian");
            if (salary > 0) ForceExpense(salary, LedgerCategory.Gaji, "Gaji karyawan (" + Data.staff.Count + " orang)");
            if (maintenance > 0) ForceExpense(maintenance, LedgerCategory.Perawatan, "Perawatan peralatan");

            bool rent = RentDueToday();
            if (rent) ForceExpense(Catalog.RentPerWeek, LedgerCategory.Sewa, "Sewa tempat mingguan");

            if (Data.cleanliness < 25f)
            {
                AddReputation(-1.5f);
                Notify("Warteg kotor! Reputasi turun.", 2);
            }

            DailyReport rep = BuildReport();
            Data.history.Add(rep);
            if (rep.profit > Data.stats.bestDayProfit) Data.stats.bestDayProfit = rep.profit;
            Data.stats.daysPlayed++;
            Data.day = Time.Day;

            if (DayReport != null) DayReport.Show(rep, rent, electricity, water, salary, maintenance);
            SaveGame(true);
        }

        DailyReport BuildReport()
        {
            DailyReport rep = new DailyReport();
            rep.day = Time != null ? Time.Day : Data.day;
            rep.revenue = Data.dayRevenue;
            rep.expense = Data.dayExpense;
            rep.profit = Data.dayRevenue - Data.dayExpense;
            rep.customersServed = Data.dayCustomersServed;
            rep.customersLost = Data.dayCustomersLost;
            rep.avgSatisfaction = Data.daySatisfactionCount > 0 ? Data.daySatisfactionSum / Data.daySatisfactionCount : 0f;
            rep.weather = (int)Time.Weather;
            rep.bestSeller = BestSellerToday();
            return rep;
        }

        string BestSellerToday()
        {
            string best = "-";
            int bestQty = 0;
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                int q = SoldToday(Catalog.Recipes[i].id);
                if (q > bestQty) { bestQty = q; best = Catalog.Recipes[i].name; }
            }
            return bestQty > 0 ? best + " (" + bestQty + " porsi)" : "-";
        }

        public int SoldToday(string recipeId)
        {
            for (int i = 0; i < Data.soldToday.Count; i++)
                if (Data.soldToday[i].id == recipeId) return Data.soldToday[i].qty;
            return 0;
        }

        /// <summary>Dipanggil tombol "Hari Berikutnya" pada laporan harian.</summary>
        public void ContinueToNextDay()
        {
            // reset akumulator harian
            Data.dayRevenue = 0;
            Data.dayExpense = 0;
            Data.dayCustomersServed = 0;
            Data.dayCustomersLost = 0;
            Data.daySatisfactionSum = 0f;
            Data.daySatisfactionCount = 0;
            Data.soldToday.Clear();
            endOfDayHandled = false;

            Time.StartNextDay();
            if (DayReport != null) DayReport.Close();
        }
    }
}

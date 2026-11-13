using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Rekrut, gaji, dan penempatan karyawan.</summary>
    public class StaffManager : MonoBehaviour
    {
        public readonly List<StaffAI> Staff = new List<StaffAI>();
        public Transform root;
        List<StaffSave> candidates = new List<StaffSave>();
        int candidateDay = -1;

        public void Init()
        {
            if (root == null) root = BuildKit.Empty(transform, "Staff", Vector3.zero).transform;
        }

        public void Rebuild(SaveData data)
        {
            for (int i = Staff.Count - 1; i >= 0; i--)
                if (Staff[i] != null) Destroy(Staff[i].gameObject);
            Staff.Clear();

            for (int i = 0; i < data.staff.Count; i++)
            {
                StaffSave s = data.staff[i];
                GameObject go = new GameObject("Karyawan_" + s.name);
                go.transform.SetParent(root, false);
                StaffAI ai = go.AddComponent<StaffAI>();
                ai.Init(s, HomePosition(i));
                Staff.Add(ai);
            }
        }

        public static Vector3 HomePosition(int index)
        {
            return new Vector3(1.2f + (index % 4) * 0.9f, 0f, -2.2f - (index / 4) * 0.9f);
        }

        public int Count { get { return GameManager.I.Data.staff.Count; } }

        public bool HasRole(StaffRole role)
        {
            for (int i = 0; i < Staff.Count; i++) if (Staff[i] != null && Staff[i].role == role) return true;
            return false;
        }

        public int RoleCount(StaffRole role)
        {
            int n = 0;
            for (int i = 0; i < Staff.Count; i++) if (Staff[i] != null && Staff[i].role == role) n++;
            return n;
        }

        /// <summary>Kandidat karyawan hari ini (deterministik supaya konsisten saat dibuka berulang).</summary>
        public List<StaffSave> GetCandidates()
        {
            int day = GameManager.I.Time != null ? GameManager.I.Time.Day : 1;
            if (candidateDay == day && candidates.Count > 0) return candidates;
            candidateDay = day;
            candidates.Clear();

            int seed = day * 7717 + 31;
            System.Random rng = new System.Random(seed);
            int count = 3 + (GameManager.I.Level >= 4 ? 1 : 0);

            for (int i = 0; i < count; i++)
            {
                StaffRole role = (StaffRole)((i + day) % 4);
                StaffSave s = new StaffSave();
                s.id = "staff_" + day + "_" + i;
                s.role = (int)role;
                s.name = Catalog.FirstNames[rng.Next(0, Catalog.FirstNames.Length)] + " " +
                         Catalog.LastNames[rng.Next(0, Catalog.LastNames.Length)];
                s.skill = Mathf.Round(30f + (float)rng.NextDouble() * 60f);
                s.speed = Mathf.Round((0.75f + (float)rng.NextDouble() * 0.7f) * 100f) / 100f;
                s.level = 1 + Mathf.FloorToInt(s.skill / 25f);
                s.salary = Mathf.RoundToInt(BaseSalary(role) * (0.75f + s.skill / 160f) / 500f) * 500;
                s.hiredDay = day;
                candidates.Add(s);
            }
            return candidates;
        }

        public static int BaseSalary(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Koki: return 95000;
                case StaffRole.Kasir: return 80000;
                case StaffRole.Pelayan: return 75000;
                default: return 60000;
            }
        }

        public static int HireCost(StaffSave s)
        {
            return Mathf.RoundToInt(s.salary * 1.5f / 1000f) * 1000;   // uang jaminan / rekrutmen
        }

        public bool Hire(StaffSave candidate)
        {
            if (GameManager.I.Data.staff.Count >= MaxStaff)
            {
                GameManager.I.Notify("Karyawan sudah maksimal (" + MaxStaff + " orang).", 2);
                return false;
            }
            int cost = HireCost(candidate);
            if (!GameManager.I.SpendMoney(cost, LedgerCategory.Gaji, "Rekrut " + Catalog.RoleName((StaffRole)candidate.role) + " " + candidate.name))
                return false;

            StaffSave copy = new StaffSave();
            copy.id = candidate.id + "_" + Random.Range(1000, 9999);
            copy.name = candidate.name;
            copy.role = candidate.role;
            copy.salary = candidate.salary;
            copy.speed = candidate.speed;
            copy.skill = candidate.skill;
            copy.level = candidate.level;
            copy.hiredDay = GameManager.I.Time != null ? GameManager.I.Time.Day : 1;
            GameManager.I.Data.staff.Add(copy);

            string n = copy.name; int r = copy.role;
            Rebuild(GameManager.I.Data);
            GameManager.I.Notify("Berhasil merekrut " + n + " sebagai " + Catalog.RoleName((StaffRole)r) + "!", 1);
            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.LevelUp, 0.7f);
            return true;
        }

        public int MaxStaff { get { return 2 + GameManager.I.Level; } }

        public bool Fire(string staffId)
        {
            StaffSave target = null;
            for (int i = 0; i < GameManager.I.Data.staff.Count; i++)
                if (GameManager.I.Data.staff[i].id == staffId) { target = GameManager.I.Data.staff[i]; break; }
            if (target == null) return false;

            int severance = Mathf.RoundToInt(target.salary * 2f / 1000f) * 1000;
            GameManager.I.Data.staff.Remove(target);
            GameManager.I.ForceExpensePublic(severance, LedgerCategory.Gaji, "Pesangon " + target.name);
            Rebuild(GameManager.I.Data);
            GameManager.I.Notify(target.name + " diberhentikan (pesangon " + Catalog.Money(severance) + ").", 0);
            return true;
        }
    }
}

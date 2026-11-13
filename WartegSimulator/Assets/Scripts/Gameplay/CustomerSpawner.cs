using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Mendatangkan pelanggan otomatis berdasarkan reputasi, level, jam, dan cuaca.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public readonly List<CustomerAI> Customers = new List<CustomerAI>();

        Transform root;
        float timer;
        bool spawning;
        int dayCounter;

        public int CustomerCount { get { return Customers.Count; } }

        public void Init()
        {
            if (root == null) root = BuildKit.Empty(transform, "Customers", Vector3.zero).transform;
            timer = 3f;
            spawning = true;
        }

        public void ResetSpawner()
        {
            ClearAllCustomers();
            timer = 3.5f;
            spawning = true;
        }

        public void StopSpawning() { spawning = false; }

        public void Unregister(CustomerAI c)
        {
            Customers.Remove(c);
            if (GameManager.I.Hud != null) GameManager.I.Hud.RefreshOrderList();
        }

        public void ClearAllCustomers()
        {
            for (int i = Customers.Count - 1; i >= 0; i--)
                if (Customers[i] != null) Destroy(Customers[i].gameObject);
            Customers.Clear();
        }

        void Update()
        {
            GameManager gm = GameManager.I;
            if (gm == null || !gm.IsPlaying || gm.IsPaused) return;
            if (gm.Time == null || !gm.Time.IsOpen) return;
            if (!spawning) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            timer = NextInterval();
            TrySpawn();
        }

        float NextInterval()
        {
            GameManager gm = GameManager.I;
            float demand = gm.DemandFactor();

            float phaseMul = 1f;
            switch (Catalog.Phase(gm.Time.Hour))
            {
                case DayPhase.Pagi: phaseMul = 0.85f; break;
                case DayPhase.Siang: phaseMul = 1.3f; break;
                case DayPhase.Sore: phaseMul = 1.05f; break;
                case DayPhase.Malam: phaseMul = 0.8f; break;
            }

            float baseInterval = 14f;
            float interval = baseInterval / Mathf.Max(0.15f, demand * phaseMul);
            return Mathf.Clamp(interval, 2.5f, 34f);
        }

        void TrySpawn()
        {
            WorldBuilder w = GameManager.I.World;
            if (w == null) return;

            int freeSeats = w.FreeSeatCount();
            if (freeSeats <= 0) return;
            if (Customers.Count >= Mathf.Max(2, w.Tables.Count * Catalog.SeatsPerTable)) return;

            CustomerKind kind = ChooseKind();
            int party = Random.Range(Catalog.Archetype(kind).partyMin, Catalog.Archetype(kind).partyMax + 1);
            party = Mathf.Min(party, freeSeats);

            for (int i = 0; i < party; i++)
            {
                Vector3 pos = w.DoorOutside + new Vector3(Random.Range(-1.6f, 1.6f), 0f, Random.Range(0.5f, 3.5f));
                GameObject go = new GameObject("Customer");
                go.transform.SetParent(root, false);
                CustomerAI c = go.AddComponent<CustomerAI>();
                c.Init(kind, pos, root);
                Customers.Add(c);
            }

            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Bell, 0.6f);
            if (GameManager.I.Hud != null) GameManager.I.Hud.RefreshOrderList();
        }

        /// <summary>Jenis pelanggan berbeda menurut waktu (pagi/siang/sore/malam).</summary>
        CustomerKind ChooseKind()
        {
            int[] weights;
            switch (Catalog.Phase(GameManager.I.Time.Hour))
            {
                case DayPhase.Pagi:
                    weights = new[] { 18, 26, 20, 16, 20, 0 }; break;
                case DayPhase.Siang:
                    weights = new[] { 34, 10, 20, 20, 14, 2 }; break;
                case DayPhase.Sore:
                    weights = new[] { 12, 28, 22, 16, 18, 4 }; break;
                default:
                    weights = new[] { 8, 12, 24, 20, 28, 8 }; break;
            }

            int total = 0;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            int roll = Random.Range(0, Mathf.Max(1, total));
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0) return (CustomerKind)i;
            }
            return CustomerKind.Warga;
        }

        public int WaitingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Customers.Count; i++)
                    if (Customers[i] != null && Customers[i].WantsService) n++;
                return n;
            }
        }

        public List<CustomerAI> WaitingCustomers()
        {
            List<CustomerAI> list = new List<CustomerAI>();
            for (int i = 0; i < Customers.Count; i++)
                if (Customers[i] != null && Customers[i].WantsService) list.Add(Customers[i]);
            return list;
        }

        public CustomerAI FindWaitingCustomer(StaffAI staff)
        {
            CustomerAI best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Customers.Count; i++)
            {
                CustomerAI c = Customers[i];
                if (c == null || !c.WantsService) continue;
                float d = Vector3.Distance(staff.transform.position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }
    }
}

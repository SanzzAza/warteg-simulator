using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Karyawan yang bekerja otomatis sesuai posisi:
    /// Koki memasak, Pelayan menyajikan, Kasir mempercepat pembayaran, Kebersihan menyapu.
    /// </summary>
    public class StaffAI : MonoBehaviour
    {
        public StaffSave data;
        public StaffRole role;
        public CharacterRig rig;

        enum Task { Idle, Walking, Working }
        Task task = Task.Idle;

        Vector3 homePos;
        Vector3 targetPos;
        float workTimer;
        float thinkTimer;
        CookingStation cookTarget;
        CustomerAI serveTarget;
        Vector3 cleanTarget;

        float SpeedMul { get { return data != null ? Mathf.Clamp(data.speed, 0.6f, 1.8f) : 1f; } }
        float SkillMul { get { return data != null ? Mathf.Lerp(0.7f, 1.4f, Mathf.Clamp01(data.skill / 100f)) : 1f; } }

        public void Init(StaffSave save, Vector3 home)
        {
            data = save;
            role = (StaffRole)save.role;
            homePos = home;
            transform.position = home;

            Color shirt;
            switch (role)
            {
                case StaffRole.Koki: shirt = new Color(0.95f, 0.95f, 0.93f); break;
                case StaffRole.Kasir: shirt = new Color(0.35f, 0.55f, 0.85f); break;
                case StaffRole.Pelayan: shirt = new Color(0.90f, 0.75f, 0.30f); break;
                default: shirt = new Color(0.45f, 0.72f, 0.45f); break;
            }
            rig = CharacterBuilder.Build(transform, "rig", shirt, new Color(0.18f, 0.20f, 0.26f), 1f);
            rig.transform.localPosition = Vector3.zero;
            // topi koki untuk koki
            if (role == StaffRole.Koki)
                BuildKit.Cyl(rig.head, "chefHat", new Vector3(0f, 0.62f, 0f), new Vector3(0.9f, 0.25f, 0.9f),
                    Color.white, 0.4f);

            gameObject.name = "Karyawan_" + save.name + " (" + Catalog.RoleName(role) + ")";
        }

        void Update()
        {
            GameManager gm = GameManager.I;
            if (gm == null || !gm.IsPlaying || gm.IsPaused) { if (rig != null) rig.SetMoving(false); return; }
            if (gm.Time == null || !gm.Time.IsOpen) { if (rig != null) rig.SetMoving(false); return; }

            switch (task)
            {
                case Task.Idle: TickIdle(); break;
                case Task.Walking: TickWalking(); break;
                case Task.Working: TickWorking(); break;
            }
        }

        void TickIdle()
        {
            if (rig != null) rig.SetMoving(false);
            thinkTimer -= Time.deltaTime;
            if (thinkTimer > 0f) return;
            thinkTimer = 0.5f;

            // pulang ke pos jika jauh dari rumah dan tidak ada tugas
            if (Vector3.Distance(Flat(transform.position), Flat(homePos)) > 1.2f)
            {
                targetPos = homePos;
                task = Task.Walking;
                return;
            }

            switch (role)
            {
                case StaffRole.Koki: LookForCooking(); break;
                case StaffRole.Pelayan: LookForServing(); break;
                case StaffRole.Kebersihan: LookForCleaning(); break;
                case StaffRole.Kasir: LookForCashier(); break;
            }
        }

        void LookForCooking()
        {
            KitchenManager k = GameManager.I.Kitchen;
            if (k == null) return;
            if (k.FreeStation() == null) return;
            if (!GameManager.I.Display.HasSpace()) return;
            RecipeDef r = k.PickBestRecipe();
            if (r == null) return;

            CookingStation st = k.FreeStation();
            cookTarget = st;
            targetPos = st.InteractPoint;
            task = Task.Walking;
        }

        void LookForServing()
        {
            CustomerSpawner sp = GameManager.I.Spawner;
            if (sp == null) return;
            CustomerAI c = sp.FindWaitingCustomer(this);
            if (c == null) return;
            serveTarget = c;
            targetPos = c.transform.position;
            task = Task.Walking;
        }

        void LookForCleaning()
        {
            if (GameManager.I.Cleanliness > 92f) return;
            cleanTarget = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-3f, 3f));
            targetPos = cleanTarget;
            task = Task.Walking;
        }

        void LookForCashier()
        {
            // kasir menjaga meja kasir; efek pasif: pelanggan lebih sabar & tip lebih besar
            targetPos = homePos;
            task = Task.Walking;
        }

        void TickWalking()
        {
            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 0.35f)
            {
                if (rig != null) rig.SetMoving(false);
                StartWork();
                return;
            }
            Vector3 step = delta.normalized * 2.4f * SpeedMul * Time.deltaTime;
            transform.position += step;
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            if (rig != null) { rig.SetMoving(true); rig.FaceDirection(delta); }
        }

        void StartWork()
        {
            switch (role)
            {
                case StaffRole.Koki:
                    if (cookTarget != null && !cookTarget.Busy)
                    {
                        KitchenManager k = GameManager.I.Kitchen;
                        RecipeDef r = k != null ? k.PickBestRecipe() : null;
                        if (r != null && cookTarget.StartCook(r, 1, true))
                        {
                            task = Task.Idle;
                            thinkTimer = 0.6f;
                            return;
                        }
                    }
                    task = Task.Idle;
                    thinkTimer = 1f;
                    break;

                case StaffRole.Pelayan:
                    if (serveTarget != null && serveTarget.WantsService)
                    {
                        serveTarget.TryServeFromDisplay();
                        if (JuiceFX.I != null)
                            JuiceFX.I.PopText(transform.position + Vector3.up * 1.8f, "Diantar!", new Color(0.7f, 0.95f, 0.8f), 0.8f);
                    }
                    serveTarget = null;
                    task = Task.Idle;
                    thinkTimer = 0.4f;
                    break;

                case StaffRole.Kebersihan:
                    workTimer = Mathf.Max(0.8f, 2.0f / SkillMul);
                    task = Task.Working;
                    break;

                default:
                    task = Task.Idle;
                    thinkTimer = 2f;
                    break;
            }
        }

        void TickWorking()
        {
            workTimer -= Time.deltaTime;
            if (workTimer > 0f) return;

            if (role == StaffRole.Kebersihan)
            {
                GameManager.I.CleanWarteg(6f * SkillMul);
                if (JuiceFX.I != null)
                    JuiceFX.I.Burst(transform.position + Vector3.up * 0.6f, new Color(0.6f, 0.85f, 1f), 5);
            }
            task = Task.Idle;
            thinkTimer = 1.2f;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
    }
}

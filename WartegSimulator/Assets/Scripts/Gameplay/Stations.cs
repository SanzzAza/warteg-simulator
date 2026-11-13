using UnityEngine;

namespace Warteg
{
    /// <summary>Pasar tempat membeli bahan mentah (buka tab Pasar).</summary>
    public class MarketStall : InteractableBase
    {
        public void Init(Transform t)
        {
            label = "Pasar Bahan";
            radius = 2.4f;
            pointOffset = new Vector3(0f, 0f, -1.6f);
            EnsureClickCollider(new Vector3(2.5f, 1.8f, 1.6f), new Vector3(0f, 0.9f, 0f));
        }

        public override void Interact(PlayerController player)
        {
            if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Pasar);
        }
    }

    /// <summary>Rak gudang bahan (buka tab gudang).</summary>
    public class StorageRack : InteractableBase
    {
        public void Init()
        {
            label = "Gudang Bahan";
            radius = 2f;
            pointOffset = new Vector3(0.9f, 0f, 0f);
            EnsureClickCollider(new Vector3(1.6f, 2.4f, 1.0f), new Vector3(0f, 1.2f, 0.3f));
        }

        public override void Interact(PlayerController player)
        {
            if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Gudang);
        }
    }

    /// <summary>Wastafel: aksi membersihkan warteg.</summary>
    public class CleaningStation : InteractableBase
    {
        float cooldown;

        public void Init()
        {
            label = "Cuci & Bersihkan";
            radius = 1.9f;
            pointOffset = new Vector3(0.9f, 0f, 0f);
            EnsureClickCollider(new Vector3(1.4f, 1.6f, 1.0f), new Vector3(0f, 0.8f, 0f));
        }

        void Update() { if (cooldown > 0f) cooldown -= Time.deltaTime; }

        public override bool CanInteract { get { return cooldown <= 0f; } }

        public override void Interact(PlayerController player)
        {
            if (cooldown > 0f) { GameManager.I.Notify("Tunggu sebentar...", 0); return; }
            if (GameManager.I.Cleanliness >= 99.5f)
            {
                GameManager.I.Notify("Warteg sudah bersih!", 1);
                return;
            }
            cooldown = 2.5f;
            float gain = 22f;
            GameManager.I.CleanWarteg(gain);
            GameManager.I.AddXp(3f);
            GameManager.I.Notify("Membersihkan warteg... kebersihan +" + Mathf.RoundToInt(gain), 1);
            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Clean);
            if (JuiceFX.I != null)
            {
                JuiceFX.I.Burst(transform.position + Vector3.up * 1.2f, new Color(0.6f, 0.85f, 1f), 10);
                JuiceFX.I.PopText(transform.position + Vector3.up * 1.6f, "Bersih!", new Color(0.6f, 0.9f, 1f));
            }
        }
    }

    /// <summary>Meja kasir: membuka laporan keuangan.</summary>
    public class CashierDesk : InteractableBase
    {
        public void Init()
        {
            label = "Kasir";
            radius = 1.9f;
            pointOffset = new Vector3(1.1f, 0f, 0f);
            EnsureClickCollider(new Vector3(1.2f, 1.4f, 2.1f), new Vector3(0f, 0.7f, 0f));
        }

        public override void Interact(PlayerController player)
        {
            if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Keuangan);
        }
    }
}

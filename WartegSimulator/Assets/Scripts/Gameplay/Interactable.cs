using UnityEngine;

namespace Warteg
{
    /// <summary>Objek yang bisa diklik / didekati pemain.</summary>
    public interface IInteractable
    {
        string Label { get; }
        Vector3 InteractPoint { get; }
        float InteractRadius { get; }
        void Interact(PlayerController player);
        bool CanInteract { get; }
    }

    /// <summary>Base helper: menambahkan collider klik + label melayang.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        public string label = "Interaksi";
        public float radius = 1.8f;
        public Vector3 pointOffset = new Vector3(0f, 0f, 0.9f);

        TextMesh hint;

        public virtual string Label { get { return label; } }
        public virtual float InteractRadius { get { return radius; } }
        public virtual bool CanInteract { get { return true; } }

        public virtual Vector3 InteractPoint
        {
            get { return transform.position + transform.rotation * pointOffset; }
        }

        public abstract void Interact(PlayerController player);

        public void EnsureClickCollider(Vector3 size, Vector3 center)
        {
            BoxCollider bc = GetComponent<BoxCollider>();
            if (bc == null) bc = gameObject.AddComponent<BoxCollider>();
            bc.size = size;
            bc.center = center;
            bc.isTrigger = false;
        }

        public void ShowHint(string text)
        {
            if (hint == null)
            {
                hint = BuildKit.WorldText(transform, "hint", text, new Vector3(0f, 2.2f, 0f), 0.09f, Color.white);
            }
            hint.text = text;
            hint.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void HideHint() { if (hint != null) hint.gameObject.SetActive(false); }
    }
}

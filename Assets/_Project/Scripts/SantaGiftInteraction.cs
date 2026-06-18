using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SantaGiftInteraction : MonoBehaviour
{
    [Header("Santa Gift Settings")]
    [SerializeField] private int repellentGiftAmount = 1;
    [SerializeField] private int medicineGiftAmount = 1;
    [SerializeField] private bool disableAfterUsed = true;

    private bool hasBeenUsed = false;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable simpleInteractable;
    private ParticleSystem[] particles;

    private void Awake()
    {
        simpleInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.AddListener(OnSantaInteracted);
        }
    }

    private void OnDisable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.RemoveListener(OnSantaInteracted);
        }
    }

    private void OnSantaInteracted(SelectEnterEventArgs args)
    {
        if (hasBeenUsed)
        {
            Debug.Log("Santa gift already claimed.");
            return;
        }

        if (InventoryService.Instance == null)
        {
            Debug.LogWarning("InventoryService not found, cannot claim gift.");
            return;
        }

        hasBeenUsed = true;

        string failReason;
        bool allOk = true;

        if (repellentGiftAmount > 0)
        {
            if (!InventoryService.Instance.TryAddItem(ItemId.FloralWater, repellentGiftAmount, out failReason))
            {
                Debug.LogWarning("FloralWater gift failed: " + failReason);
                allOk = false;
            }
        }

        if (medicineGiftAmount > 0)
        {
            if (!InventoryService.Instance.TryAddItem(ItemId.HerbMedicine, medicineGiftAmount, out failReason))
            {
                Debug.LogWarning("HerbMedicine gift failed: " + failReason);
                allOk = false;
            }
        }

        if (allOk)
            Debug.Log("Santa gift claimed: FloralWater +" + repellentGiftAmount + ", HerbMedicine +" + medicineGiftAmount);
        else
            Debug.Log("Santa gift partially claimed, inventory may be full.");

        StopSantaEffects();

        if (disableAfterUsed && simpleInteractable != null)
        {
            simpleInteractable.enabled = false;
        }
    }

    private void StopSantaEffects()
    {
        foreach (ParticleSystem p in particles)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

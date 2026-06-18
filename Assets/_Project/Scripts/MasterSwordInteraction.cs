using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MasterSwordInteraction : MonoBehaviour
{
    [Header("Master Sword Settings")]
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
            simpleInteractable.selectEntered.AddListener(OnSwordInteracted);
        }
    }

    private void OnDisable()
    {
        if (simpleInteractable != null)
        {
            simpleInteractable.selectEntered.RemoveListener(OnSwordInteracted);
        }
    }

    private void OnSwordInteracted(SelectEnterEventArgs args)
    {
        if (hasBeenUsed)
        {
            Debug.Log("Master Sword already used.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not found, cannot grant revive.");
            return;
        }

        hasBeenUsed = true;

        GameManager.Instance.GrantReviveChance();

        Debug.Log("Master Sword: revive chance granted.");

        StopSwordEffects();

        if (disableAfterUsed && simpleInteractable != null)
        {
            simpleInteractable.enabled = false;
        }
    }

    private void StopSwordEffects()
    {
        foreach (ParticleSystem p in particles)
        {
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

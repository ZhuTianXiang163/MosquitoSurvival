using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FlowerCollectible : MonoBehaviour
{
    [Header("Collect Settings")]
    [SerializeField] private int flowerAmount = 1;
    [SerializeField] private float respawnTime = 180f;

    private bool isCollected = false;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer[] renderers;
    private Collider[] colliders;
    private ParticleSystem[] particles;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isCollected)
        {
            return;
        }

        CollectFlower(args);
    }

    private void CollectFlower(SelectEnterEventArgs args = null)
    {
        if (InventoryService.Instance == null)
        {
            Debug.LogWarning("InventoryService not found in scene.");
            CancelGrab(args);
            return;
        }

        string failReason;
        if (InventoryService.Instance.TryAddItem(ItemId.Flower, flowerAmount, out failReason))
        {
            Debug.Log("Flower collected (+1 Flower).");
        }
        else
        {
            Debug.LogWarning("Flower collect failed: " + failReason);
            ToastPopupController.Instance?.Show("Backpack is full!");
            CancelGrab(args);
            return;
        }

        StartCoroutine(HideAndRespawn());
    }

    private void CancelGrab(SelectEnterEventArgs args)
    {
        if (args != null && grabInteractable != null)
        {
            grabInteractable.interactionManager.SelectExit(
                args.interactorObject, args.interactableObject);
        }
    }

    private IEnumerator HideAndRespawn()
    {
        isCollected = true;

        SetFlowerActive(false);

        yield return new WaitForSeconds(respawnTime);

        SetFlowerActive(true);

        isCollected = false;

        Debug.Log("Flower respawned.");
    }

    private void SetFlowerActive(bool active)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }

        foreach (Collider c in colliders)
        {
            c.enabled = active;
        }

        foreach (ParticleSystem p in particles)
        {
            if (active)
            {
                p.Play();
            }
            else
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (grabInteractable != null)
        {
            grabInteractable.enabled = active;
        }
    }
}

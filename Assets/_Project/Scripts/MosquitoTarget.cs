using UnityEngine;

public class MosquitoTarget : MonoBehaviour
{
    [Header("Hit Feedback")]
    public GameObject hitEffectPrefab;
    public float hitEffectScale = 3.0f;
    public AudioClip hitSound;
    public float hitSoundVolume = 0.8f;

    [Header("Reward Feedback")]
    public GameObject rewardTextPrefab;
    public int rewardAmount = 80;

    public bool IsDead { get; private set; }

    /// <summary>
    /// 强制击杀（不触发奖励），用于喷雾清场等场景。
    /// 复用 Hit 的视觉效果和清理逻辑，但不发金币。
    /// </summary>
    public void ForceKill()
    {
        if (IsDead) return;
        IsDead = true;

        Debug.Log("MosquitoTarget: force killed.");

        // 关闭碰撞
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 停止音效
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in audioSources)
        {
            audio.Stop();
            audio.enabled = false;
        }

        // 隐藏模型
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // 关闭移动脚本
        MosquitoMovement movement = GetComponent<MosquitoMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }
    }

    public void Hit(Vector3 hitPoint)
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        Debug.Log("Mosquito hit!");
        Debug.Log("MosquitoTarget: registering mosquito kill.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterMosquitoKill(rewardAmount);
        }
        else
        {
            Debug.LogWarning("MosquitoTarget: GameManager.Instance is null.");
        }

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            effect.transform.localScale = Vector3.one * hitEffectScale;
            Destroy(effect, 2f);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hitPoint, hitSoundVolume);
        }

        if (rewardTextPrefab != null)
        {
            Vector3 textPosition = hitPoint + new Vector3(0.12f, 0.12f, 0f);
            GameObject textObj = Instantiate(rewardTextPrefab, textPosition, Quaternion.identity);

            FloatingRewardText floatingText = textObj.GetComponent<FloatingRewardText>();
            if (floatingText != null)
            {
                floatingText.Setup(rewardAmount);
            }
        }

        // 关闭碰撞，避免重复击中
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 停止音效
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audio in audioSources)
        {
            audio.Stop();
            audio.enabled = false;
        }

        // 隐藏模型
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // 关闭我们自己写的移动脚本
        MosquitoMovement movement = GetComponent<MosquitoMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        // 注意：不要 Destroy(gameObject)
        // 注意：不要 gameObject.SetActive(false)
        // Anything World 的行为树可能还会访问 Transform，销毁/禁用会报错
    }
}

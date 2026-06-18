using UnityEngine;

public class MosquitoSpawnZone : MonoBehaviour
{
    public float weight = 1f;

    public Vector3 GetRandomPoint()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
        {
            Debug.LogWarning(name + " has no BoxCollider！");
            return transform.position;
        }

        Vector3 localPoint = new Vector3(
            Random.Range(-box.size.x * 0.5f, box.size.x * 0.5f),
            Random.Range(-box.size.y * 0.5f, box.size.y * 0.5f),
            Random.Range(-box.size.z * 0.5f, box.size.z * 0.5f)
        );

        localPoint += box.center;

        return transform.TransformPoint(localPoint);
    }
}

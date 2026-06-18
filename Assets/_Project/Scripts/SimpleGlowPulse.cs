using UnityEngine;

public class SimpleGlowPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float minScale = 0.15f;
    public float maxScale = 0.3f;

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = new Vector3(s, s, s);
    }
}

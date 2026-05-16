using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    void Awake() => Instance = this;

    public void Shake(float duration = 0.4f, float magnitude = 0.2f)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        Vector3 origin = transform.localPosition;
        float t = 0f;

        while (t < duration)
        {
            float damp = 1f - (t / duration);
            transform.localPosition = origin + (Vector3)Random.insideUnitCircle * magnitude * damp;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = origin;
    }
}

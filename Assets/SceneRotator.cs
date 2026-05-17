using System.Collections;
using UnityEngine;

public class SceneRotator : MonoBehaviour
{
    private StarPlotter starPlotter;
    private int        step;
    private Coroutine  rotCoroutine;

    void Start()
    {
        starPlotter = FindObjectOfType<StarPlotter>();
    }

    public void Rotate()
    {
        if (starPlotter == null) return;
        step = (step + 1) % 4;

        if (rotCoroutine != null) StopCoroutine(rotCoroutine);
        rotCoroutine = StartCoroutine(LerpRotation(step * 90f));
    }

    private IEnumerator LerpRotation(float targetZ)
    {
        const float duration = 0.5f;
        Quaternion from = starPlotter.transform.rotation;
        Quaternion to   = Quaternion.Euler(0f, 0f, targetZ);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            starPlotter.transform.rotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        starPlotter.transform.rotation = to;
        rotCoroutine = null;
    }
}

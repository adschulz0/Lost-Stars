using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneRotator : MonoBehaviour
{
    public StarPlotter starPlotter;

    [Header("Buttons")]
    public Button shiftXButton;
    public Button shiftYButton;
    public Button shiftZButton;
    public Button resetButton;

    public float lerpDuration = 1.5f;

    private Coroutine rotationCoroutine;

    void Start()
    {
        shiftXButton.onClick.AddListener(() => RotateTo(Quaternion.Euler(90f, 0f, 0f)));
        shiftYButton.onClick.AddListener(() => RotateTo(Quaternion.Euler(0f, 90f, 0f)));
        shiftZButton.onClick.AddListener(() => RotateTo(Quaternion.Euler(0f, 0f, 90f)));
        resetButton.onClick.AddListener(() => RotateTo(Quaternion.identity));
    }

    private void RotateTo(Quaternion target)
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        rotationCoroutine = StartCoroutine(LerpRotation(target));
    }

    private IEnumerator LerpRotation(Quaternion target)
    {
        Quaternion start   = starPlotter.transform.rotation;
        float      elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            starPlotter.transform.rotation = Quaternion.Slerp(start, target,
                Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration));
            yield return null;
        }
        starPlotter.transform.rotation = target;
        rotationCoroutine = null;
    }
}

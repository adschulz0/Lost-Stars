using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float movementSpeed = 10.0f;
    float fastMovementSpeed = 50f;
    public float freeLookSensitivity = 3.0f;
    public float zoomSensitivity = 10.0f;
    public float fastZoomSensitivity = 50.0f;
    public float clusterViewDistance = 500f;

    private bool looking = false;
    private POINT savedCursorPos;

    private bool      isLerping;
    private Coroutine lerpCoroutine;

    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X; public int Y; }

    private void Awake()
    {
        fastMovementSpeed = movementSpeed * 2f;
        transform.position = new Vector3(0f, 1f, 1000f);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // Lerps the camera to clusterViewDistance from clusterWorldPos, facing it.
    // Does nothing if already within that distance.
    public void LerpToCluster(Vector3 clusterWorldPos)
    {
        Vector3 dir = transform.position - clusterWorldPos;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.back;

        Vector3    targetPos = clusterWorldPos + dir.normalized * clusterViewDistance;
        Quaternion targetRot = Quaternion.LookRotation(clusterWorldPos - targetPos);

        if (lerpCoroutine != null) StopCoroutine(lerpCoroutine);
        lerpCoroutine = StartCoroutine(LerpCoroutine(targetPos, targetRot));
    }

    private IEnumerator LerpCoroutine(Vector3 targetPos, Quaternion targetRot)
    {
        const float duration = 1.5f;
        isLerping = true;
        Vector3    startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        transform.SetPositionAndRotation(targetPos, targetRot);
        isLerping     = false;
        lerpCoroutine = null;
    }

    void Update()
    {
        if (isLerping) return;
        // Toggle looking mode with the right mouse button
        if (Input.GetMouseButtonDown(1))
        {
            GetCursorPos(out savedCursorPos);
            looking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(1))
        {
            looking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetCursorPos(savedCursorPos.X, savedCursorPos.Y);
        }

        // Handle movement
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastMovementSpeed : movementSpeed;
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
        {
            transform.position += transform.up * speed * Time.deltaTime;
        }

        // Handle rotation
        if (looking)
        {
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        // Handle zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            float zoomSpeed = Input.GetKey(KeyCode.LeftShift) ? fastZoomSensitivity : zoomSensitivity;
            transform.position += transform.forward * scrollInput * zoomSpeed * Time.deltaTime;
        }
    }
}

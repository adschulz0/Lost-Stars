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

    private bool looking = false;
    private POINT savedCursorPos;

    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT pt);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X; public int Y; }
    private void Awake()
    {
        fastMovementSpeed = movementSpeed * 2f;
    }
    void Update()
    {
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

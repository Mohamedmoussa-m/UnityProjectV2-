using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float sensitivity = 2f;

    [Header("Optional – Assign Player Body (for yaw rotation)")]
    public Transform playerBody;

    float xRotation = 0f;

    void Start()
    {
        // Hide and lock the cursor to the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Get mouse movement
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * 10f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * 10f * Time.deltaTime;

        // Calculate pitch (up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // prevent flipping

        // Apply pitch rotation to camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Apply yaw rotation to player body if assigned
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            // Rotate camera itself if no parent body assigned
            transform.Rotate(Vector3.up * mouseX, Space.World);
        }
    }
}

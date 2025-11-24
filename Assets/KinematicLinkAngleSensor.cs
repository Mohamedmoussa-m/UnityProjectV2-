using UnityEngine;

/// <summary>
/// Measures the joint angle for a revolute joint based on local rotation.
/// It automatically determines the initial 0-degree offset when the game starts.
/// </summary>
public class KinematicLinkAngleSensor : MonoBehaviour
{
    // NOTE: This should be set to X based on your console diagnostics!
    public enum JointAxis { X, Y, Z }

    [Tooltip("The local axis of rotation for this link that corresponds to the joint axis.")]
    public JointAxis rotationAxis = JointAxis.X;

    [Header("Calibration (Read Only)")]
    [Tooltip("The raw local angle that the joint was at when the game started (Your 0-degree reference).")]
    [SerializeField]
    private float initialRotationOffset = 0f;

    [Tooltip("Check this to see live angle readings in the Console.")]
    public bool debugAngles = true; // Setting this to TRUE by default to help you verify the fix

    // The calculated joint angle in degrees (accessible by the logger)
    public float CurrentAngle { get; private set; } = 0f;

    void Start()
    {
        // CRITICAL: Auto-capture the current raw rotation as the starting offset (0 degrees).
        // This ensures that whatever position the robot is in when Play is pressed becomes the 0-degree point.
        Vector3 rawAngles = transform.localRotation.eulerAngles;

        // Find the raw angle value for the set rotation axis
        switch (rotationAxis)
        {
            case JointAxis.X:
                initialRotationOffset = rawAngles.x;
                break;
            case JointAxis.Y:
                initialRotationOffset = rawAngles.y;
                break;
            case JointAxis.Z:
                initialRotationOffset = rawAngles.z;
                break;
        }

        Debug.Log($"[CALIBRATION] {gameObject.name} initialized. Offset on {rotationAxis} is set to: {initialRotationOffset:F2}");
    }

    void LateUpdate()
    {
        // Use LateUpdate to ensure we capture the rotation *after* the control scripts have run.
        CalculateJointAngle();
    }

    /// <summary>
    /// Measures the angle of this Link's Transform relative to its Parent Link's Transform, 
    /// accounting for the initial offset.
    /// </summary>
    private void CalculateJointAngle()
    {
        // Raw angles of this link relative to its parent.
        Vector3 rawAngles = transform.localRotation.eulerAngles;
        float currentRawAngle = 0;

        // 1. Get the current raw angle on the selected axis.
        switch (rotationAxis)
        {
            case JointAxis.X:
                currentRawAngle = rawAngles.x;
                break;
            case JointAxis.Y:
                currentRawAngle = rawAngles.y;
                break;
            case JointAxis.Z:
                currentRawAngle = rawAngles.z;
                break;
        }

        // 2. Calculate the difference (the actual joint movement from the starting point)
        float angle = currentRawAngle - initialRotationOffset;

        // 3. Normalize and store the final angle.
        CurrentAngle = NormalizeAngle(angle);

        if (debugAngles)
        {
            // Debug now shows the calibrated angle and the raw value it came from.
            Debug.Log($"Sensor on {gameObject.name}: Raw({rotationAxis})={currentRawAngle:F1} | Calibrated Angle: {CurrentAngle:F1}");
        }
    }

    /// <summary>
    /// Converts angle from 0-360 to -180 to 180.
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        // This is a robust way to handle the 360/0 wrap-around caused by the offset subtraction
        angle %= 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;
        return angle;
    }
}
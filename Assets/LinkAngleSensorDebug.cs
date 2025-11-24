using UnityEngine;

public class LinkAngleSensorDebug : MonoBehaviour
{
    public enum JointAxis { X, Y, Z }

    [Tooltip("The local axis of rotation for this link.")]
    public JointAxis rotationAxis = JointAxis.Y;

    [Tooltip("Set this if your joint starts at 90 or -90 degrees but should read as 0.")]
    public float initialRotationOffset = 0f;

    [Header("Debugging")]
    [Tooltip("Check this to see X, Y, Z angles in the Console while playing.")]
    public bool debugAngles = true;

    // The value read by the Logger
    public float CurrentAngle { get; private set; } = 0f;

    void LateUpdate()
    {
        CalculateJointAngle();
    }

    private void CalculateJointAngle()
    {
        Vector3 rawAngles = transform.localRotation.eulerAngles;
        float angle = 0;

        switch (rotationAxis)
        {
            case JointAxis.X:
                angle = rawAngles.x;
                break;
            case JointAxis.Y:
                angle = rawAngles.y;
                break;
            case JointAxis.Z:
                angle = rawAngles.z;
                break;
        }

        angle -= initialRotationOffset;
        CurrentAngle = NormalizeAngle(angle);

        if (debugAngles)
        {
            // Look at the Console to see which value (X, Y, or Z) is actually changing!
            Debug.Log($"Sensor on {gameObject.name}: Raw(X,Y,Z)= {rawAngles.x:F1}, {rawAngles.y:F1}, {rawAngles.z:F1} | Angle: {CurrentAngle:F1}");
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360;
        if (angle > 180) angle -= 360;
        else if (angle < -180) angle += 360;
        return angle;
    }
}
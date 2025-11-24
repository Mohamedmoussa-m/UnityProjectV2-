using UnityEngine;
using TMPro;

public class TargetSphereSensor : MonoBehaviour
{
    [Header("Who is the end effector? (by tag)")]
    public string endEffectorTag = "EndEffector";

    [Header("Sensor Output")]
    public float lastDistance;               // scalar error
    public Vector3 lastContactWorld;         // contact point world
    public Vector3 lastContactLocal;         // contact point local

    [Header("Optional – Local Label Above Sphere")]
    public TMP_Text worldSpaceLabel;

    [Header("Optional – Global HUD Reference")]
    public TMP_Text globalHUD;               // <- assign your main TelemetryPanel TMP here

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(endEffectorTag))
            return;

        // compute contact point and distance
        Vector3 contactWorld = other.ClosestPoint(transform.position);

        lastContactWorld = contactWorld;
        lastContactLocal = transform.InverseTransformPoint(contactWorld);
        lastDistance = Vector3.Distance(transform.position, contactWorld);

        // update local floating label (optional)
        if (worldSpaceLabel)
            worldSpaceLabel.text = $"{lastDistance:F3} m";

        // OPTIONAL — update the big HUD TMP
        if (globalHUD)
        {
            globalHUD.text =
                $"Sphere Error\n" +
                $"Distance: {lastDistance:F3} m\n" +
                $"Local X: {lastContactLocal.x:F3}\n" +
                $"Local Y: {lastContactLocal.y:F3}\n" +
                $"Local Z: {lastContactLocal.z:F3}";
        }

        // optional color feedback
        float t = Mathf.InverseLerp(0f, 0.2f, lastDistance);
        var rend = GetComponent<Renderer>();
        if (rend) rend.material.color = Color.Lerp(Color.green, Color.red, t);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(endEffectorTag))
            return;

        // clear local label
        if (worldSpaceLabel)
            worldSpaceLabel.text = "";

        // clear global HUD only if this sphere is the one updating it
        if (globalHUD)
            globalHUD.text = "";
    }
}

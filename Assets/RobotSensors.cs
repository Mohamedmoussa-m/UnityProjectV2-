using UnityEngine;
using TMPro;

public class RobotSensors : MonoBehaviour
{
    [Header("Robot Definition")]
    public AutoJointBuilder builder;      // drag the object with AutoJointBuilder here

    [Header("End Effector Transform (TCP)")]
    public Transform endEffector;         // your tool-tip / gripper frame

    [Header("Optional UI Output")]
    public TMP_Text debugText;            // TextMeshPro to show values on screen (optional)

    [HideInInspector] public ArticulationBody[] joints;

    void Awake()
    {
        BuildJointListFromBuilder();
    }

    void BuildJointListFromBuilder()
    {
        if (builder == null)
        {
            Debug.LogError("RobotSensors: builder reference is not set.");
            return;
        }

        // This mirrors AutoJointBuilder.BuildJoints() child ordering
        Transform[] all = builder.GetComponentsInChildren<Transform>(includeInactive: true);
        if (all.Length <= 1)
        {
            Debug.LogError("RobotSensors: not enough children under builder.");
            return;
        }

        int jointCount = all.Length - 1; // same idea as AutoJointBuilder
        joints = new ArticulationBody[jointCount];

        for (int i = 1; i < all.Length; i++)
        {
            Transform child = all[i];
            var ab = child.GetComponent<ArticulationBody>();
            if (ab == null)
            {
                Debug.LogWarning($"RobotSensors: {child.name} has no ArticulationBody (was BuildJoints run?).");
            }
            joints[i - 1] = ab;
            if (ab != null)
                Debug.Log($"RobotSensors: Joint {i - 1} mapped to {ab.name}");
        }
    }

    void Update()
    {
        if (joints == null || joints.Length == 0) return;

        float[] qDeg = GetJointAnglesDegrees();
        Vector3 eePos = GetEEPosition();
        Vector3 eeRot = GetEERotation();

        if (debugText)
        {
            string ui = "Joint Angles (deg):\n";
            for (int i = 0; i < qDeg.Length; i++)
                ui += $"J{i}: {qDeg[i]:F1}°\n";

            ui += $"\nEE Pos: {eePos:F3}\nEE Rot: {eeRot:F1}";
            debugText.text = ui;
        }
    }

    // -------- Public helpers for CSV / MATLAB --------

    public float[] GetJointAnglesDegrees()
    {
        if (joints == null) return new float[0];

        float[] q = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null || joints[i].dofCount == 0)
            {
                q[i] = 0f;
            }
            else
            {
                // ArticulationBody.jointPosition[0] is the true joint angle (rad)
                q[i] = Mathf.Rad2Deg * joints[i].jointPosition[0];
            }
        }

        return q;
    }

    public Vector3 GetEEPosition()
    {
        return endEffector ? endEffector.position : Vector3.zero;
    }

    public Vector3 GetEERotation()
    {
        return endEffector ? endEffector.eulerAngles : Vector3.zero;
    }
}

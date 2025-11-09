using UnityEngine;

[ExecuteInEditMode] // lets it run in Editor without pressing Play
public class AutoJointBuilder : MonoBehaviour
{
    [System.Serializable]
    public class JointConfig
    {
        public ArticulationJointType jointType = ArticulationJointType.RevoluteJoint;
        public Vector3 anchorPosition = Vector3.zero; // local to the child link
        public Vector3 axis = Vector3.up;             // joint rotation axis (child link space)
    }

    [Tooltip("Configure one entry per child link (so 5 DOF = 5 entries).")]
    public JointConfig[] joints;

    [Tooltip("Rebuild on Start/compile. Leave ON for now.")]
    public bool rebuildOnStart = true;

    void Start()
    {
        if (!Application.isPlaying && !rebuildOnStart) return;
        BuildJoints();
    }

    [ContextMenu("Build Joints Now")]
    public void BuildJoints()
    {
        var all = GetComponentsInChildren<Transform>(includeInactive: true);
        if (all.Length <= 1)
        {
            Debug.LogWarning("[AutoJointBuilder] Need Base + at least one child link.");
            return;
        }

        int needed = all.Length - 1;
        if (joints == null || joints.Length != needed)
        {
            joints = new JointConfig[needed];
            for (int i = 0; i < needed; i++) joints[i] = new JointConfig();
            Debug.Log($"[AutoJointBuilder] Generated {needed} joint slots. Fill anchors, then rebuild.");
            return;
        }

        var rootAB = GetComponent<ArticulationBody>() ?? gameObject.AddComponent<ArticulationBody>();
        rootAB.immovable = true;
        rootAB.jointType = ArticulationJointType.FixedJoint;

        for (int i = 1; i < all.Length; i++)
        {
            Transform child = all[i];
            var cfg = joints[i - 1];

            var ab = child.GetComponent<ArticulationBody>() ?? child.gameObject.AddComponent<ArticulationBody>();

            ab.jointType = cfg.jointType;
            ab.anchorPosition = cfg.anchorPosition;

            // align anchor so that local X axis is the desired rotation axis
            if (cfg.axis == Vector3.up)
                ab.anchorRotation = Quaternion.Euler(0, 0, 90);
            else if (cfg.axis == Vector3.forward)
                ab.anchorRotation = Quaternion.Euler(0, -90, 0);
            else // default (axis = X)
                ab.anchorRotation = Quaternion.identity;

            ab.parentAnchorPosition = Vector3.zero;

            ab.linearLockX = ArticulationDofLock.LockedMotion;
            ab.linearLockY = ArticulationDofLock.LockedMotion;
            ab.linearLockZ = ArticulationDofLock.LockedMotion;

            ab.twistLock = ArticulationDofLock.LimitedMotion;

            var drive = ab.xDrive;
            drive.lowerLimit = -180f;
            drive.upperLimit = 180f;
            drive.stiffness = 10000f;
            drive.damping = 100f;
            drive.forceLimit = 1000f;
            ab.xDrive = drive;

            Debug.Log($"[AutoJointBuilder] {child.name}: {ab.jointType}, axis {cfg.axis}, anchor {cfg.anchorPosition}");
        }

        Debug.Log("[AutoJointBuilder] Build complete.");
    }
}

using UnityEngine;

/// <summary>
/// Builds and configures a chain of ArticulationBody joints for a robotic manipulator.
/// Each entry in the "chain" defines one joint (base → tip).
/// </summary>
[ExecuteAlways]
public class AutoJointBuilderV2 : MonoBehaviour
{
    [System.Serializable]
    public class JointDef
    {
        [Header("Parent / Child Links")]
        public Transform parentLink;               // null for base
        public Transform childLink;                // must exist

        [Header("World-space geometry")]
        public Vector3 pivotWorld;                 // hinge position in world space
        public Vector3 axisWorld = Vector3.right;  // hinge axis in world space

        [Header("Rotation limits (deg)")]
        public Vector2 limitsDeg = new Vector2(-170f, 170f);
    }

    [Header("Joint Chain (Base → Tip)")]
    [Tooltip("Fill one entry per joint. For a 5-DOF robot use 6 entries: base + 5 revolute joints.")]
    public JointDef[] chain;

    [Header("Drive Settings")]
    public float stiffness = 3000f;
    public float damping = 300f;
    public float forceLimit = 1000f;

    [Header("Auto-build on Play")]
    public bool buildOnPlay = true;

    void Start()
    {
        if (Application.isPlaying && buildOnPlay)
            Build();
    }

    [ContextMenu("Build Now")]
    public void Build()
    {
        if (chain == null || chain.Length == 0)
        {
            Debug.LogError("[AutoJointBuilderV2] Please fill the 'chain' array in the Inspector.");
            return;
        }

        Debug.Log($"[AutoJointBuilderV2] Building {chain.Length} articulation joints …");

        for (int i = 0; i < chain.Length; i++)
        {
            var jd = chain[i];
            if (!jd.childLink)
            {
                Debug.LogError($"[AutoJointBuilderV2] chain[{i}] is missing a childLink.");
                continue;
            }

            // Ensure ArticulationBody on the child
            var ab = jd.childLink.GetComponent<ArticulationBody>();
            if (!ab)
                ab = jd.childLink.gameObject.AddComponent<ArticulationBody>();

            if (i == 0 || jd.parentLink == null)
            {
                // Base link = fixed
                ab.jointType = ArticulationJointType.FixedJoint;
                ab.immovable = true;
                continue;
            }

            // Ensure parent also has an ArticulationBody
            var parentAB = jd.parentLink.GetComponent<ArticulationBody>();
            if (!parentAB)
                parentAB = jd.parentLink.gameObject.AddComponent<ArticulationBody>();

            ab.jointType = ArticulationJointType.RevoluteJoint;
            ab.immovable = false;

            // --- Anchors ---
            Vector3 pWorld = jd.pivotWorld;
            ab.anchorPosition = jd.childLink.InverseTransformPoint(pWorld);
            ab.parentAnchorPosition = jd.parentLink.InverseTransformPoint(pWorld);

            // --- Axis alignment (align local X to desired world axis) ---
            Vector3 axisChildLocal = jd.childLink.InverseTransformDirection(jd.axisWorld).normalized;
            Vector3 axisParentLocal = jd.parentLink.InverseTransformDirection(jd.axisWorld).normalized;
            ab.anchorRotation = Quaternion.FromToRotation(Vector3.right, axisChildLocal);
            ab.parentAnchorRotation = Quaternion.FromToRotation(Vector3.right, axisParentLocal);

            // --- Lock translations and non-twist rotations ---
            ab.linearLockX = ArticulationDofLock.LockedMotion;
            ab.linearLockY = ArticulationDofLock.LockedMotion;
            ab.linearLockZ = ArticulationDofLock.LockedMotion;
            ab.swingYLock = ArticulationDofLock.LockedMotion;
            ab.swingZLock = ArticulationDofLock.LockedMotion;
            ab.twistLock = ArticulationDofLock.LimitedMotion;

            // --- Drive setup ---
            var drive = ab.xDrive;
            drive.lowerLimit = jd.limitsDeg.x;
            drive.upperLimit = jd.limitsDeg.y;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            ab.xDrive = drive;

            // --- Extra damping for stability ---
            ab.linearDamping = 0.05f;
            ab.angularDamping = 0.05f;
            ab.jointFriction = 1f;
        }

        Debug.Log("[AutoJointBuilderV2] Build complete ✅");
    }
}

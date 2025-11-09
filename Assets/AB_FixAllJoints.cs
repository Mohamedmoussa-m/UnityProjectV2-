using System.Linq;
using UnityEngine;

public class AB_FixAllJoints : MonoBehaviour
{
    [Header("Assign in order [Base, Link1..Link5_Tool]. If empty, auto-detects.")]
    public ArticulationBody[] links;

    [Header("Optional hinge markers (empties at joint centers): J1..J4, etc.")]
    public Transform[] hingeHints;

    [Header("Drive strength (hinge motor)")]
    public float stiffness = 12000f;
    public float damping = 400f;
    public float forceLimit = 1000000f;

    [Header("Body damping (small values calm jitter)")]
    public float linearDamping = 0.1f;
    public float angularDamping = 0.1f;

    [Header("Physics hygiene")]
    public bool baseImmovable = true;
    public bool useGravityOnLinks = false;

    [Header("Click (or tick) to run once")]
    public bool ApplyNow = false;

    // Optional button in context menu
    [ContextMenu("Run Fix")]
    public void RunFixContextMenu() { ApplyFix(); }

    private void OnValidate()
    {
        if (ApplyNow)
        {
            ApplyNow = false;
            ApplyFix();
        }
    }

    public void ApplyFix()
    {
        // 1) Gather links if not provided
        if (links == null || links.Length == 0)
        {
            links = GetComponentsInChildren<ArticulationBody>(true)
                    .OrderBy(ab => GetDepth(ab.transform))
                    .ToArray();
        }
        if (links == null || links.Length < 2)
        {
            Debug.LogError("[AB_FixAllJoints] Need at least Base + 1 child ArticulationBody.");
            return;
        }

        // 2) Warn about non-1 scales (can distort anchors/inertia)
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            Vector3 s = t.localScale;
            if (Vector3.Distance(s, Vector3.one) > 1e-4f)
                Debug.LogWarning($"[AB_FixAllJoints] Non-1 scale on '{t.name}' = {s}. Prefer (1,1,1).");
        }

        // 3) Base setup
        var baseAB = links[0];
        baseAB.jointType = ArticulationJointType.FixedJoint;
        baseAB.immovable = baseImmovable;
        baseAB.useGravity = false;
        baseAB.linearDamping = linearDamping;
        baseAB.angularDamping = angularDamping;

        // 4) Children setup: hinge-only rotation, anchors matched
        for (int i = 1; i < links.Length; i++)
        {
            var child = links[i];

            // Parent: try real Transform parent; fallback to previous link
            var parent = child.transform.parent ? child.transform.parent.GetComponent<ArticulationBody>() : null;
            if (!parent) parent = links[i - 1];

            // Joint type + hygiene
            child.jointType = ArticulationJointType.RevoluteJoint;
            child.useGravity = useGravityOnLinks;
            child.linearDamping = linearDamping;
            child.angularDamping = angularDamping;

            // Lock all linear motion
            child.linearLockX = ArticulationDofLock.LockedMotion;
            child.linearLockY = ArticulationDofLock.LockedMotion;
            child.linearLockZ = ArticulationDofLock.LockedMotion;

            // Allow ONLY twist (Unity’s X drive). Lock swings.
            child.twistLock = ArticulationDofLock.LimitedMotion;
            child.swingYLock = ArticulationDofLock.LockedMotion;
            child.swingZLock = ArticulationDofLock.LockedMotion;

            // Strong drive (limits in degrees)
            var d = child.xDrive;
            // If limits look unset (both zero), apply usable defaults
            if (Mathf.Approximately(d.lowerLimit, 0f) && Mathf.Approximately(d.upperLimit, 0f))
            {
                bool midJoint = (i == 2 || i == 3 || i == 4);
                d.lowerLimit = midJoint ? -120f : -180f;
                d.upperLimit = midJoint ? 120f : 180f;
            }
            d.stiffness = stiffness;
            d.damping = damping;
            d.forceLimit = forceLimit;
            d.target = Mathf.Clamp(d.target, d.lowerLimit, d.upperLimit);
            d.targetVelocity = 0f;
            child.xDrive = d;

            // === Anchors: compute a single world hinge point, then set on CHILD ===
            Vector3 worldHinge = ChooseWorldHinge(i - 1, parent, child);

            // Correct pattern: child stores both anchors
            child.parentAnchorPosition = parent.transform.InverseTransformPoint(worldHinge);
            child.anchorPosition = child.transform.InverseTransformPoint(worldHinge);
            child.parentAnchorRotation = Quaternion.identity;
            child.anchorRotation = Quaternion.identity;

            // Optional check in Console: world mismatch distance
            float mismatch = Vector3.Distance(
                parent.transform.TransformPoint(child.parentAnchorPosition),
                child.transform.TransformPoint(child.anchorPosition)
            );
            if (mismatch > 0.002f)
                Debug.LogWarning($"[AB_FixAllJoints] {child.name} anchor mismatch {mismatch:F3} m — move hinge hint or adjust pivots.");
        }

        Debug.Log("[AB_FixAllJoints] Done. Joints are hinge-only and linked to parents.");
    }

    // Pick a hinge point in world space:
    // 1) use hingeHints[jointIndex] if provided;
    // 2) else use child's pivot (common authoring pattern);
    // 3) else midpoint between collider centers (rough fallback).
    private Vector3 ChooseWorldHinge(int jointIndex, ArticulationBody parent, ArticulationBody child)
    {
        if (hingeHints != null &&
            jointIndex >= 0 &&
            jointIndex < hingeHints.Length &&
            hingeHints[jointIndex] != null)
        {
            return hingeHints[jointIndex].position;
        }

        Vector3 candidate = child.transform.position;

        Collider pc = parent ? parent.GetComponent<Collider>() : null;
        Collider cc = child.GetComponent<Collider>();
        if (pc && cc)
            candidate = (pc.bounds.center + cc.bounds.center) * 0.5f;

        return candidate;
    }

    private static int GetDepth(Transform t)
    {
        int d = 0; while (t && t.parent != null) { d++; t = t.parent; }
        return d;
    }
}

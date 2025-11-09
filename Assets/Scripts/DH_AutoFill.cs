using UnityEngine;

/// Computes world pivots & axes from your DH table (θi = 0 "zero pose")
/// and fills AutoJointBuilderV2.chain for a 5-DOF (all-revolute) arm.
/// Put this on the SAME GameObject that has AutoJointBuilderV2.
[ExecuteAlways]
public class DH_AutoFill : MonoBehaviour
{
    [Header("Link Transforms (Base + 5 children in order)")]
    public Transform baseLink; // fixed base
    public Transform link1;    // child of base (after J1)
    public Transform link2;    // child of link1 (after J2)
    public Transform link3;    // child of link2 (after J3)
    public Transform link4;    // child of link3 (after J4)
    public Transform link5;    // child of link4 (after J5) -> can be an empty "Tool" GameObject

    [Header("Link lengths (meters) from your DH table")]
    public float L1 = 0.300f;  // (used visually if you want to offset models; not needed for pivots here)
    public float L2 = 0.400f;
    public float L3 = 0.400f;
    public float L4 = 0.300f;

    [Header("Choose DH base axes in WORLD (Unity default: Y=up, X=right)")]
    public Vector3 worldZ_DH0 = Vector3.up;      // z0 : J1 axis (vertical by default)
    public Vector3 worldX_DH0 = Vector3.right;   // x0 : orthonormal to z0

    private AutoJointBuilderV2 builder;

    // ---------- DH helpers (standard DH: Rz(theta) * Tz(d) * Tx(a) * Rx(alpha)) ----------
    static Matrix4x4 DH(float thetaDeg, float d, float a, float alphaDeg)
    {
        float th = thetaDeg * Mathf.Deg2Rad;
        float al = alphaDeg * Mathf.Deg2Rad;
        float cth = Mathf.Cos(th), sth = Mathf.Sin(th);
        float cal = Mathf.Cos(al), sal = Mathf.Sin(al);

        Matrix4x4 T = Matrix4x4.identity;
        T.m00 = cth; T.m01 = -sth * cal; T.m02 = sth * sal; T.m03 = a * cth;
        T.m10 = sth; T.m11 = cth * cal; T.m12 = -cth * sal; T.m13 = a * sth;
        T.m20 = 0f; T.m21 = sal; T.m22 = cal; T.m23 = d;
        return T;
    }

    static Matrix4x4 BasisTR(Vector3 origin, Vector3 x, Vector3 z)
    {
        Vector3 X = x.normalized;
        Vector3 Z = z.normalized;
        Vector3 Y = Vector3.Cross(Z, X).normalized; // right-handed
        Matrix4x4 M = Matrix4x4.identity;
        M.SetColumn(0, new Vector4(X.x, Y.x, Z.x, 0));
        M.SetColumn(1, new Vector4(X.y, Y.y, Z.y, 0));
        M.SetColumn(2, new Vector4(X.z, Y.z, Z.z, 0));
        M.SetColumn(3, new Vector4(origin.x, origin.y, origin.z, 1));
        return M;
    }

    static Vector3 Col3(Matrix4x4 m) => new Vector3(m.m03, m.m13, m.m23);
    static Vector3 AxisZ(Matrix4x4 m) => new Vector3(m.m02, m.m12, m.m22);

    [ContextMenu("Populate Builder From DH (zero pose)")]
    public void Populate()
    {
        // Find the builder on the same GameObject
        builder = GetComponent<AutoJointBuilderV2>();
        if (!builder) { Debug.LogError("AutoJointBuilderV2 not found on this GameObject."); return; }

        // Check required transforms
        if (!baseLink || !link1 || !link2 || !link3 || !link4 || !link5)
        {
            Debug.LogError("Assign baseLink, link1..link5 in the Inspector (Link5 can be an empty 'Tool' under Link4).");
            return;
        }

        // ---- Build zero-pose DH frames ----
        // Base world frame uses your chosen world axes at baseLink.position
        Matrix4x4 T0 = BasisTR(baseLink.position, worldX_DH0, worldZ_DH0);

        // Your DH (from the table you shared):
        // i   alpha(i-1)  a(i-1)  d(i)   theta(i)
        // 1 :   0            0      0     θ1
        // 2 :  +90°          0      0     θ2
        // 3 :   0           L2      0     θ3
        // 4 :   0           L3      0     θ4
        // 5 :  +90°          0     L4     θ5
        // For zero pose: θi = 0
        Matrix4x4 T01 = DH(0, 0, 0, 0);
        Matrix4x4 T12 = DH(0, 0, 0, +90);
        Matrix4x4 T23 = DH(0, 0, L2, 0);
        Matrix4x4 T34 = DH(0, 0, L3, 0);
        Matrix4x4 T45 = DH(0, L4, 0, +90);

        // Cumulative frames in WORLD
        Matrix4x4 T_0_1 = T0 * T01;
        Matrix4x4 T_0_2 = T_0_1 * T12;
        Matrix4x4 T_0_3 = T_0_2 * T23;
        Matrix4x4 T_0_4 = T_0_3 * T34;
        Matrix4x4 T_0_5 = T_0_4 * T45;

        // Joint axes (standard DH: joint i rotates about z_{i-1})
        Vector3 z0 = AxisZ(T0);       // J1
        Vector3 z1 = AxisZ(T_0_1);    // J2
        Vector3 z2 = AxisZ(T_0_2);    // J3
        Vector3 z3 = AxisZ(T_0_3);    // J4
        Vector3 z4 = AxisZ(T_0_4);    // J5

        // Pivot points (origins of frames {0..4})
        Vector3 p0 = Col3(T0);
        Vector3 p1 = Col3(T_0_1);
        Vector3 p2 = Col3(T_0_2);
        Vector3 p3 = Col3(T_0_3);
        Vector3 p4 = Col3(T_0_4);

        // ---- Fill builder.chain: 6 entries = base fixed + 5 revolute joints ----
        builder.chain = new AutoJointBuilderV2.JointDef[6];

        // 0) Base (fixed)
        builder.chain[0] = new AutoJointBuilderV2.JointDef
        {
            parentLink = null,
            childLink = baseLink,
            pivotWorld = p0,        // ignored for fixed base
            axisWorld = z0,
            limitsDeg = new Vector2(0, 0)
        };

        // 1) J1: base -> link1
        builder.chain[1] = new AutoJointBuilderV2.JointDef
        {
            parentLink = baseLink,
            childLink = link1,
            pivotWorld = p0,
            axisWorld = z0,
            limitsDeg = new Vector2(-180, 180)
        };

        // 2) J2: link1 -> link2
        builder.chain[2] = new AutoJointBuilderV2.JointDef
        {
            parentLink = link1,
            childLink = link2,
            pivotWorld = p1,
            axisWorld = z1,
            limitsDeg = new Vector2(-120, 120)
        };

        // 3) J3: link2 -> link3
        builder.chain[3] = new AutoJointBuilderV2.JointDef
        {
            parentLink = link2,
            childLink = link3,
            pivotWorld = p2,
            axisWorld = z2,
            limitsDeg = new Vector2(-120, 120)
        };

        // 4) J4: link3 -> link4
        builder.chain[4] = new AutoJointBuilderV2.JointDef
        {
            parentLink = link3,
            childLink = link4,
            pivotWorld = p3,
            axisWorld = z3,
            limitsDeg = new Vector2(-180, 180)
        };

        // 5) J5: link4 -> link5 (tool)
        builder.chain[5] = new AutoJointBuilderV2.JointDef
        {
            parentLink = link4,
            childLink = link5,
            pivotWorld = p4,
            axisWorld = z4,
            limitsDeg = new Vector2(-180, 180)
        };

        Debug.Log("[DH_AutoFill] Filled AutoJointBuilderV2.chain from DH (zero pose). Open AutoJointBuilderV2 and click 'Build Now'.");
    }
}

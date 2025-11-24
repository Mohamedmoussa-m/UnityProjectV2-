using UnityEngine;

public class PlacementZone : MonoBehaviour
{
    public string acceptsTag = "Block";
    public float radius = 0.08f;
    public float heightTol = 0.05f;
    public Transform snapPoint;

    void OnDrawGizmos()
    {
        if (!snapPoint) snapPoint = transform;
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawCube(snapPoint.position, new Vector3(radius * 2, heightTol * 2, radius * 2));
    }

    public bool IsInside(Transform t)
    {
        if (!snapPoint) snapPoint = transform;
        Vector3 local = t.position - snapPoint.position;
        Vector2 xy = new Vector2(local.x, local.z);
        return xy.magnitude <= radius && Mathf.Abs(local.y) <= heightTol;
    }
}

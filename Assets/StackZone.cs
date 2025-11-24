using UnityEngine;

public class StackZone : MonoBehaviour
{
    public string acceptsTag = "Block";
    public Transform basePoint;
    public float blockSize = 0.05f;
    public int currentHeight = 0;

    void OnDrawGizmos()
    {
        if (!basePoint) basePoint = transform;
        Gizmos.color = new Color(0, 0, 1, 0.25f);
        Gizmos.DrawCube(basePoint.position + Vector3.up * (currentHeight * blockSize * 0.5f), new Vector3(blockSize, currentHeight * blockSize, blockSize));
    }

    public int ExpectedLevelYIndex() => currentHeight;

    public Vector3 LevelWorldPosition(int level)
    {
        if (!basePoint) basePoint = transform;
        return basePoint.position + Vector3.up * (blockSize * 0.5f + blockSize * level);
    }

    public bool TryRegisterPlaced(Transform t, float tolXY = 0.03f, float tolY = 0.03f)
    {
        if (!basePoint) basePoint = transform;
        if (!t.CompareTag(acceptsTag)) return false;
        int level = ExpectedLevelYIndex();
        Vector3 target = LevelWorldPosition(level);
        Vector3 delta = t.position - target;
        if (new Vector2(delta.x, delta.z).magnitude <= tolXY && Mathf.Abs(delta.y) <= tolY)
        {
            currentHeight++;
            return true;
        }
        return false;
    }
}

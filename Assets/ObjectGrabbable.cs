using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObjectGrabbable : MonoBehaviour
{
    [Tooltip("Optional unique id for logging")] public string objectId;
    [Tooltip("Points awarded when correctly placed")] public int scoreValue = 1;
}

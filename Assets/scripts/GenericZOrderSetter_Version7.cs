using UnityEngine;

public class GenericZOrderSetter : MonoBehaviour
{
    [Tooltip("If set, will offset Z relative to this transform (e.g. the player).")]
    public Transform referenceTransform;
    [Tooltip("Z offset from reference (integer only).")]
    public int zOffset = 0;

    void LateUpdate()
    {
        int targetZ = zOffset;
        if (referenceTransform != null)
            targetZ += Mathf.RoundToInt(referenceTransform.position.z);

        // Clamp to -2, -1, 0, 1, 2 only
        targetZ = Mathf.Clamp(targetZ, -2, 2);

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, targetZ);
    }
}
using UnityEngine;

public class GenericZOrderIntFollower : MonoBehaviour
{
    [Tooltip("If set, will keep an integer Z offset from this transform (e.g. the player).")]
    public Transform referenceTransform;

    [Tooltip("Z offset from reference (integer only).")]
    public int zOffset = 0;

    void LateUpdate()
    {
        if (referenceTransform != null)
        {
            // Round reference Z to nearest integer
            int refZ = Mathf.RoundToInt(referenceTransform.position.z);
            int targetZ = refZ + zOffset;

            Vector3 pos = transform.position;
            // Only update Z if changed
            if (pos.z != targetZ)
                transform.position = new Vector3(pos.x, pos.y, targetZ);
        }
    }
}
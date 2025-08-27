using UnityEngine;

public class GenericZOrderSetter : MonoBehaviour
{
    [Tooltip("If set, will offset Z relative to this transform (e.g. the player).")]
    public Transform referenceTransform;
    [Tooltip("Z offset from reference (integer only).")]
    public int zOffset = 0;

    void LateUpdate()
    {
        float refZ = 0f;
        if (referenceTransform != null)
            refZ = referenceTransform.position.z;

        // Calculate target Z, round, and clamp to allowed range
        int targetZ = Mathf.Clamp(Mathf.RoundToInt(refZ) + zOffset, -2, 2);

        Vector3 pos = transform.position;
        // Only update z if changed for efficiency
        if (!Mathf.Approximately(pos.z, targetZ))
            transform.position = new Vector3(pos.x, pos.y, targetZ);
    }
}
using UnityEngine;

public static class ZOrderUtility
{
    // Snaps any float z to the nearest allowed (integer or .5) value
    public static float SnapZ(float z)
    {
        float intPart = Mathf.Floor(z);
        float frac = z - intPart;
        if (frac < 0.25f) return intPart;
        if (frac < 0.75f) return intPart + 0.5f;
        return intPart + 1f;
    }

    // Returns a Z position offset from a base Z, using an integer or half-step offset
    public static float OffsetZ(float baseZ, int offset)
    {
        return SnapZ(baseZ + offset);
    }
}
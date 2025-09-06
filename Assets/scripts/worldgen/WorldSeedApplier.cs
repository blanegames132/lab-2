using System;
using UnityEngine;

/// <summary>
/// Applies random world seed values to an InfiniteCameraSpawnerModular instance.
/// Attach this to any GameObject and assign references in the inspector.
/// </summary>
public class WorldSeedApplier : MonoBehaviour
{
    [Header("References")]
    public InfiniteCameraSpawnerModular spawner;
    public SeedSelector seedSelector;

    /// <summary>
    /// Optionally apply seed automatically when the game starts.
    /// </summary>
    public bool applyOnStart = true;

    void Start()
    {
        if (applyOnStart)
        {
            ApplySeed();
            // Optionally, regenerate world immediately
            if (spawner != null)
                spawner.UpdateWorldIfNeeded();
        }
    }

    /// <summary>
    /// Applies random terrain parameters to the selected spawner based on the seed in SeedSelector.
    /// </summary>
    public void ApplySeed()
    {
        if (spawner == null)
        {
            Debug.LogError("WorldSeedApplier: Spawner is null!");
            return;
        }
        if (seedSelector == null)
        {
            Debug.LogError("WorldSeedApplier: SeedSelector is null!");
            return;
        }

        int hash = seedSelector.usedSeedInt;
        System.Random rand = new System.Random(hash);

        spawner.seedScale = SeededValue(rand, 0.05f, 0.2f, 1);
        spawner.seedAmplitude = SeededValue(rand, 0.6f, 3.0f, 2);
        spawner.hillHeight = SeededValue(rand, 3f, 15f, 4);
        spawner.hillCurveRandomJitter = SeededValue(rand, 0.08f, 0.7f, 5);
        spawner.hillRandomAmplitude = SeededValue(rand, 0.05f, 0.9f, 6);
        spawner.hillNoiseScale = SeededValue(rand, 0.25f, 1.3f, 7);
        spawner.curveShift = SeededValue(rand, -1.2f, 1.2f, 8);
        spawner.perlinOffsetX = SeededValue(rand, 0f, 100f, 9);
        spawner.perlinOffsetZ = SeededValue(rand, 0f, 100f, 10);
        spawner.perlinStrength = SeededValue(rand, 0.3f, 1.6f, 11);
        spawner.perlinBase = SeededValue(rand, 0f, 1.0f, 12);
        spawner.hillVerticalShift = SeededValue(rand, -2f, 2f, 13);
        spawner.cliffSharpness = SeededValue(rand, 1.5f, 3.0f, 14);

        spawner.randomHillCurve = GenerateRandomHillCurve(rand, spawner, seedSelector);
    }

    /// <summary>
    /// Returns a seeded float value in [min, max] using the given offset.
    /// </summary>
    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }

    /// <summary>
    /// Returns a seeded int value in [min, max) using the given offset.
    /// </summary>
    int SeededInt(System.Random rand, int min, int max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return rand.Next(min, max);
    }

    /// <summary>
    /// Generates a random hill curve for terrain shape.
    /// </summary>
    AnimationCurve GenerateRandomHillCurve(System.Random rand, InfiniteCameraSpawnerModular spawner, SeedSelector seedSelector)
    {
        var randomHillCurve = new AnimationCurve();
        int numKeys = SeededInt(rand, 8, 20, 17);
        for (int i = 0; i < numKeys; i++)
        {
            float t = Mathf.Lerp(0f, 1f, (float)i / (numKeys - 1));
            float canyonZ = Mathf.PerlinNoise(i * 0.23f + spawner.perlinOffsetZ, seedSelector.usedSeedInt * 0.00001f) * 2f - 1f;
            float canyonWall = Mathf.Abs(canyonZ * spawner.hillRandomAmplitude * 2f);
            bool shouldBlock = canyonWall > 0.95f;
            float baseValue = Mathf.Sin(t * Mathf.PI * SeededValue(rand, 1f, 2f, 20 + i));
            float value = baseValue * spawner.cliffSharpness + SeededValue(rand, -spawner.hillCurveRandomJitter, spawner.hillCurveRandomJitter, 100 + i);
            value = Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), spawner.cliffSharpness);
            if (shouldBlock)
            {
                value = (rand.NextDouble() > 0.5) ? spawner.cliffSharpness * 1.5f : -spawner.cliffSharpness * 1.5f;
            }
            randomHillCurve.AddKey(new Keyframe(
                t,
                Mathf.Clamp(value, -spawner.cliffSharpness * 2f, spawner.cliffSharpness * 2f)
            ));
        }
        return randomHillCurve;
    }
}
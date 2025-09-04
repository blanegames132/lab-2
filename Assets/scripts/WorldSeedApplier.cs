using System;
using UnityEngine;

[Serializable]
public class WorldSeedApplier : MonoBehaviour
{
    public void ApplySeed(TileInfiniteCameraSpawner spawner, SeedSelector seedSelector)
    {
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

    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }
    int SeededInt(System.Random rand, int min, int max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return rand.Next(min, max);
    }

    AnimationCurve GenerateRandomHillCurve(System.Random rand, TileInfiniteCameraSpawner spawner, SeedSelector seedSelector)
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
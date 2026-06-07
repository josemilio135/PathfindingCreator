using System;
using UnityEngine;

[Serializable]
public struct SamplerSettings
{
    // --- Shared ---
    public float ExtraOffset;

    // --- Convex obstacles ---
    public float MinCornerAngle;
    public float StraightAngleTolerance;

    // --- Architecture ---
    public float MinArchCornerAngle;

    public static SamplerSettings Default => new()
    {
        ExtraOffset = 0.25f,
        MinCornerAngle = 10f,
        StraightAngleTolerance = 10f,
        MinArchCornerAngle = 25f,
    };
}
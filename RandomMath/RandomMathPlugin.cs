using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using Unity.Mathematics;

[BepInPlugin("com.example.randommath", "Random Math Mod", "1.0.0")]
public class RandomMathMod : BaseUnityPlugin
{
    private Harmony harmony;
    private System.Random rng;

    private void Awake()
    {
        rng = new System.Random();
        harmony = new Harmony("com.example.randommath");
        harmony.PatchAll();
        Logger.LogInfo("Random Math Mod loaded!");
    }
}

public static class RandomMath
{
    private static readonly System.Random rng = new System.Random();

    private static float RandomFloat() => (float)(rng.NextDouble() * 100.0 - 50.0);
    private static double RandomDouble() => rng.NextDouble() * 100.0 - 50.0;

    private static void LogRandomization(string functionName, object result)
    {
        Debug.Log($"[RandomMath] {functionName} -> {result}");
    }

    [HarmonyPatch(typeof(Mathf))]
    [HarmonyPatch("PI", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool RandomPi(ref float __result)
    {
        __result = RandomFloat();
        LogRandomization("Mathf.PI", __result);
        return false;
    }

    [HarmonyPatch(typeof(Math), nameof(Math.Sin))]
    [HarmonyPrefix]
    private static bool RandomSinSystem(ref double __result)
    {
        __result = RandomDouble();
        LogRandomization("Math.Sin", __result);
        return false;
    }

    [HarmonyPatch(typeof(Mathf), nameof(Mathf.Sin))]
    [HarmonyPrefix]
    private static bool RandomSin(ref float __result)
    {
        __result = RandomFloat();
        LogRandomization("Mathf.Sin", __result);
        return false;
    }

    [HarmonyPatch(typeof(Physics), nameof(Physics.Raycast))]
    [HarmonyPrefix]
    private static bool RandomRaycast(ref bool __result)
    {
        __result = rng.Next(0, 2) == 1;
        LogRandomization("Physics.Raycast", __result);
        return false;
    }

    [HarmonyPatch(typeof(Rigidbody), "velocity", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool RandomVelocity(ref Vector3 __result)
    {
        __result = new Vector3(RandomFloat(), RandomFloat(), RandomFloat());
        LogRandomization("Rigidbody.velocity", __result);
        return false;
    }

    [HarmonyPatch(typeof(Mesh), "vertices", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool RandomVertices(ref Vector3[] __result)
    {
        __result = new Vector3[10]; // Generate 10 random vertices for example
        for (int i = 0; i < __result.Length; i++)
        {
            __result[i] = new Vector3(RandomFloat(), RandomFloat(), RandomFloat());
        }
        LogRandomization("Mesh.vertices", __result.Length);
        return false;
    }

    [HarmonyPatch(typeof(Renderer), "bounds", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool RandomRendererBounds(ref Bounds __result)
    {
        __result = new Bounds(new Vector3(RandomFloat(), RandomFloat(), RandomFloat()),
                              new Vector3(RandomFloat(), RandomFloat(), RandomFloat()));
        LogRandomization("Renderer.bounds", __result);
        return false;
    }
}

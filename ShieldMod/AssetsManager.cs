using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Shield_Mod
{
    public class AssetsManager
    {
        public static AudioClip Woosh;
        public static AudioClip PunchHeavy;

        public static GameObject BreakParticleMetalSaw;
        public static GameObject Lightning;

        public static GameObject flash;

        static bool loaded;

        public static void GetAssets()
        {
            if (loaded) return;
            Woosh = GetAddress<AudioClip>("Assets/Sounds/Weapons/PunchSwooshHeavy.wav");
            PunchHeavy = GetAddress<AudioClip>("Assets/Sounds/Weapons/punch_heavy.wav");
            BreakParticleMetalSaw = GetAddress<GameObject>("Assets/Particles/Breaks/BreakParticleMetalSaw.prefab");
            flash = GetAddress<GameObject>("Assets/Particles/Flashes/V2FlashUnparriable.prefab");
            Lightning = GetAddress<GameObject>("Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Lighting Beam Reflected.prefab");
            loaded = true;
        }

        public static T GetAddress<T>(string path)
        {
            T result = Addressables.LoadAssetAsync<T>(path).WaitForCompletion();

            return result;
        }
    }
}

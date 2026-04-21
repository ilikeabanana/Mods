using BepInEx.Configuration;
using HarmonyLib;
using PluginConfig.API;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class PlayerRandomizers
    {
        public static int MaxHealth;
        static MinMaxConfig WalkSpeedMult;
        static MinMaxConfig Health;
        public static void GenerateConfigs()
        {
            WalkSpeedMult = new MinMaxConfig("Walk Speed", 1f, Plugin.PlayerPanel);
            Health = new MinMaxConfig("Health", 100f, Plugin.PlayerPanel);
        }


        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
        [HarmonyPostfix]
        public static void ApplyChanges(NewMovement __instance)
        {
            __instance.walkSpeed *= WalkSpeedMult.GetRand;
            MonoSingleton<NewMovement>.Instance.hp = (int)Health.GetRand;
            MaxHealth = NewMovement.Instance.hp;
            //__instance.jumpPower *= Random.Range(0f, 2f);
        }


        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
        [HarmonyPostfix]
        static void HealthChange()
        {
            MonoSingleton<NewMovement>.Instance.hp = (int)Health.GetRand;
            MaxHealth = NewMovement.Instance.hp;
        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))]
        public class CheckPatch
        {
            static int HealthBefore;
            static bool Prefix(ref NewMovement __instance, int health)
            {
                HealthBefore = __instance.hp;
                return true;
            }
            static void Postfix(int health)
            {
                if (HealthBefore + health <= MaxHealth)
                {
                    MonoSingleton<NewMovement>.Instance.hp += health;
                }
                if (MonoSingleton<NewMovement>.Instance.hp > MaxHealth)
                {
                    MonoSingleton<NewMovement>.Instance.hp = MaxHealth;
                }
            }
        }
    }

    public class MinMaxConfig
    {
        RandomConfig<float> _min;
        RandomConfig<float> _max;
        public float Min
        {
            get
            {
                return _min.Value;
            }
        }
        public float Max
        {
            get
            {
                return _max.Value;
            }
        }

        public float GetRand
        {
            get
            {
                return UnityEngine.Random.Range(Min, Max);
            }
        }

        public MinMaxConfig(string name, float defaultVal, ConfigPanel panel)
        {
            _min = new RandomConfig<float>(panel, "Min " + name, defaultVal);
            _max = new RandomConfig<float>(panel, "Max " + name, defaultVal);
        }
    }
}
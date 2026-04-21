using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class AnimatorRandomizer
    {
        [HarmonyPatch(typeof(Animator), nameof(Animator.SetTrigger), new System.Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static void TriggerRandomizer(ref string name, Animator __instance)
        {
            if (!Plugin.ChangeAnimator.Value) return;
            List<string> triggerParamNames = new List<string>();
            foreach (var param in __instance.parameters)
            {
                if(param.type == AnimatorControllerParameterType.Trigger)
                    triggerParamNames.Add(param.name);
            }

            name = triggerParamNames[Random.Range(0, triggerParamNames.Count)];
        }
        [HarmonyPatch(typeof(Animator), nameof(Animator.SetInteger), new System.Type[] { typeof(string), typeof(int) })]
        [HarmonyPrefix]
        public static void IntegerRandomizer(ref string name, Animator __instance)
        {
            if (!Plugin.ChangeAnimator.Value) return;
            List<string> triggerParamNames = new List<string>();
            foreach (var param in __instance.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Int)
                    triggerParamNames.Add(param.name);
            }

            name = triggerParamNames[Random.Range(0, triggerParamNames.Count)];
        }
        [HarmonyPatch(typeof(Animator), nameof(Animator.SetBool), new System.Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        public static void BoolRandomizer(ref string name, Animator __instance)
        {
            if (!Plugin.ChangeAnimator.Value) return;
            List<string> triggerParamNames = new List<string>();
            foreach (var param in __instance.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                    triggerParamNames.Add(param.name);
            }

            name = triggerParamNames[Random.Range(0, triggerParamNames.Count)];
        }
        [HarmonyPatch(typeof(Animator), nameof(Animator.SetFloat), new System.Type[] { typeof(string), typeof(float) })]
        [HarmonyPrefix]
        public static void FloatRandomizer(ref string name, Animator __instance)
        {
            if (!Plugin.ChangeAnimator.Value) return;
            List<string> triggerParamNames = new List<string>();
            foreach (var param in __instance.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Float)
                    triggerParamNames.Add(param.name);
            }

            name = triggerParamNames[Random.Range(0, triggerParamNames.Count)];
        }
    }
}

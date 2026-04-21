using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Text;
using SettingsMenu.Components;
using SettingsMenu.Models;

namespace Ultrachaos.Randomizers
{
    public class StringRandomizer : Randomizer<string>
    {
        protected override int GetInstanceID(string item) => item.GetHashCode();
        protected override RandomConfigValue GetConfigValue() => Plugin.ChangeText.Value;

        public RandomConfigValue Config => GetConfigValue();
    }

    [HarmonyPatch]
    public static class TextRandomizer
    {
        public static bool randomizeCharacters = false;
        public static readonly StringRandomizer _randomizer = new StringRandomizer();
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private static string GetRandomized(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            _randomizer.AddToPool(input);
            Plugin.Logger.LogInfo($"Pool size: {_randomizer.Pool.Count}");

            if (_randomizer.Pool.Count <= 1)
                return input;

            if (randomizeCharacters)
            {
                StringBuilder randomizedText = new StringBuilder();
                for (int i = 0; i < input.Length; i++)
                {
                    char c = chars[UnityEngine.Random.Range(0, chars.Length)];
                    randomizedText.Append(c);
                }
                return randomizedText.ToString();
            }
            string random = _randomizer.GetRandom(input);

            return random;
        }

        public static void SearchTexts()
        {
            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var text in texts)
            {
                if (!Plugin.CanChangeObj(text.gameObject)) continue;
                text.text = GetRandomized(text.text);
            }
            Text[] textUgly = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in textUgly)
            {
                if (!Plugin.CanChangeObj(text.gameObject)) continue;
                text.text = GetRandomized(text.text);
            }
        }

        [HarmonyPatch(typeof(HudMessageReceiver), nameof(HudMessageReceiver.SendHudMessage))]
        [HarmonyPrefix]
        public static void ReplaceFunnyTexts(ref string newmessage, ref string newmessage2)
        {
            newmessage = GetRandomized(newmessage);
            newmessage2 = GetRandomized(newmessage2);
        }

        [HarmonyPatch(typeof(Readable), nameof(Readable.Awake))]
        [HarmonyPrefix]
        public static void ReplaceReadTexts(Readable __instance)
        {
            __instance.content = GetRandomized(__instance.content);
        }

        [HarmonyPatch(typeof(LevelNamePopup), nameof(LevelNamePopup.NameAppear))]
        [HarmonyPrefix]
        public static void PopUp(LevelNamePopup __instance)
        {
            __instance.layerString = GetRandomized(__instance.layerString);
            __instance.nameString = GetRandomized(__instance.nameString);
        }

        [HarmonyPatch(typeof(SettingsPageBuilder), nameof(SettingsPageBuilder.BuildPage))]
        [HarmonyPrefix]
        public static void Pagetext(SettingsPage settingsPage)
        {
            foreach (var cat in settingsPage.categories)
            {
                cat.title = GetRandomized(cat.title);
                cat.description = GetRandomized(cat.description);
                foreach (var item in cat.items)
                {
                    item.sideNote = GetRandomized(item.sideNote);
                    item.buttonLabel = GetRandomized(item.buttonLabel);
                    item.label = GetRandomized(item.label);
                }
            }
        }

        [HarmonyPatch(typeof(EnemyInfoPage), nameof(EnemyInfoPage.DisplayInfo), new System.Type[] { typeof(SpawnableObject) })]
        [HarmonyPostfix]
        public static void InfoUpdate(EnemyInfoPage __instance)
        {
            __instance.enemyEntryTitle.text = GetRandomized(__instance.enemyEntryTitle.text);
            __instance.enemyPageTitle.text = GetRandomized(__instance.enemyPageTitle.text);
            __instance.enemyPageContent.text = GetRandomized(__instance.enemyPageContent.text);
        }

        [HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Update))]
        [HarmonyPostfix]
        public static void HPUpdate(HealthBar __instance)
        {
            __instance.hpText.text = GetRandomized(NewMovement.Instance.hp.ToString());
        }

        public static void CheatName(ref string __result)
        {
            __result = GetRandomized(__result);
        }

        [HarmonyPatch(typeof(SubtitleController), nameof(SubtitleController.DisplaySubtitle), new System.Type[] { typeof(string), typeof(AudioSource), typeof(bool) })]
        [HarmonyPrefix]
        public static void HPUpdate(ref string caption)
        {
            caption = GetRandomized(caption);
        }

        [HarmonyPatch(typeof(ShopZone), nameof(ShopZone.Start))]
        [HarmonyPostfix]
        public static void ShopeZoneTip(ShopZone __instance)
        {
            if (__instance.tipOfTheDay == null) return;
            __instance.tipOfTheDay.text = GetRandomized(__instance.tipOfTheDay.text);
        }
        [HarmonyPatch(typeof(DiscordController), nameof(DiscordController.SendActivity))]
        [HarmonyPrefix]
        public static void RandomActivity(DiscordController __instance)
        {
            __instance.cachedActivity.Details = GetRandomized(__instance.cachedActivity.Details);
            __instance.cachedActivity.Assets.SmallText = GetRandomized(__instance.cachedActivity.Assets.SmallText);
            __instance.cachedActivity.State = GetRandomized(__instance.cachedActivity.State);
            __instance.cachedActivity.Name = GetRandomized(__instance.cachedActivity.Name);
            __instance.cachedActivity.Assets.LargeText = GetRandomized(__instance.cachedActivity.Assets.LargeText);
        }
        static Dictionary<BossHealthBarTemplate, string> changedTemps = new Dictionary<BossHealthBarTemplate, string>();

        [HarmonyPatch(typeof(BossHealthBarTemplate), nameof(BossHealthBarTemplate.Initialize))]
        [HarmonyPostfix]
        public static void InitBossName(BossHealthBarTemplate __instance)
        {
            if (_randomizer.Config == RandomConfigValue.Disabled) return;
            string text = GetRandomized(__instance.bossNameText.text);
            if (changedTemps.TryGetValue(__instance, out string val))
            {
                text = val;
            }
            else
            {
                changedTemps.Add(__instance, text);
            }

            __instance.bossNameText.text = text;
            foreach (var txt in __instance.textInstances)
            {
                txt.text = text;
            }
        }

        [HarmonyPatch(typeof(BossHealthBarTemplate), nameof(BossHealthBarTemplate.ChangeName))]
        [HarmonyPostfix]
        public static void ChangeBossName(BossHealthBarTemplate __instance)
        {
            if (_randomizer.Config == RandomConfigValue.Disabled) return;
            string text = GetRandomized(__instance.bossNameText.text);
            if (changedTemps.TryGetValue(__instance, out string val))
            {
                text = val;
            }
            else
            {
                changedTemps.Add(__instance, text);
            }

            __instance.bossNameText.text = text;
            foreach (var txt in __instance.textInstances)
            {
                txt.text = text;
            }
        }
    }
}
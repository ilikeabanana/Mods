using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class ImageRandomizer : Randomizer<Sprite>
    {
        public static readonly ImageRandomizer Instance = new ImageRandomizer();

        protected override int GetInstanceID(Sprite item) => item.GetInstanceID();
        protected override RandomConfigValue GetConfigValue() => Plugin.ChangeImages.Value;

        public override void Initialize()
        {
            Plugin.OnInstantiateMethod.Add((obj) =>
            {
                foreach (var img in obj.GetComponentsInChildren<Image>())
                {
                    ReplaceRenderer(img);
                }
            });
        }
        public static void ApplyChanges()
        {
            Instance.ChangeMats();
        }

        public void ChangeMats()
        {
            AddRangeToPool(Resources.FindObjectsOfTypeAll<Sprite>());
            Image[] imgs = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Plugin.Logger.LogInfo("Randomizing images...");

            foreach (Image rend in imgs)
            {
                if (rend.gameObject.layer == 19) continue;

                ReplaceRenderer(rend);
            }
        }

        public void ReplaceRenderer(Image rend)
        {
            try
            {
                if (rend == null) return;
                if (Plugin.ChangeImages.Value == RandomConfigValue.Disabled) return;
                if (!Plugin.CanChangeObj(rend.gameObject)) return;
                rend.sprite = ReplaceMaterial(rend.sprite);
            }
            catch (System.Exception) { }
        }

        public Sprite ReplaceMaterial(Sprite mat)
        {
            try
            {
                AddToPool(mat);
                return GetRandom(mat);
            }
            catch (System.Exception) { return mat; }
        }
    }
}
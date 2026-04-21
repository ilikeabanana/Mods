using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    [HarmonyPatch]
    public class MaterialRandomizer : Randomizer<Material>
    {
        public static readonly MaterialRandomizer Instance = new MaterialRandomizer();

        protected override int GetInstanceID(Material item) => item.GetInstanceID();
        protected override RandomConfigValue GetConfigValue() => Plugin.ChangeMaterials.Value;

        public override void Initialize()
        {
            Plugin.OnInstantiateMethod.Add((obj) =>
            {
                foreach (var rend in obj.GetComponentsInChildren<Renderer>())
                {
                    ReplaceRenderer(rend);
                }
            });
        }
        public static IEnumerator ApplyChanges()
        {
            yield return new WaitForSeconds(0.1f);
            Instance.ChangeMats();
        }

        public void ChangeMats()
        {
            if (Plugin.ChangeMaterials.Value == RandomConfigValue.Disabled) return;

            AddRangeToPool(Resources.FindObjectsOfTypeAll<Material>());

            foreach (Renderer rend in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (rend.gameObject.GetComponent<ParticleSystem>()) continue;
                if (rend.gameObject.layer == 19) continue;
                ReplaceRenderer(rend);
            }
        }

        public void ReplaceRenderer(Renderer rend)
        {
            try
            {
                if (rend == null) return;
                if (Plugin.ChangeMaterials.Value == RandomConfigValue.Disabled) return;

                rend.SetPropertyBlock(null);

                Material[] mats = rend.sharedMaterials;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null)
                        mats[i] = GetRandom(mats[i]);
                }

                rend.sharedMaterials = mats;
            }
            catch (System.Exception) { }
        }


        public Material ReplaceMaterial(Material mat)
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{
    public class MaterialSwapper
    {
        public static Material snow;
        public static Material ice;

        public static UnityAction OnFinish;

        /// <summary>
        /// Water: I mean, yeah thats water
        /// Mulch: the mulch flowing in prelude
        /// Corpses: the water in 5-2
        /// Tornado: vortex in 5-4
        /// Vein and Flesh: to turn gluttony into an icy enviroment
        /// </summary>
        static readonly string[] waterNames = { "water", "mulch", "corpses", "tornado", "mouth", "flesh", "vein", "minced", "faces" };

        /// <summary>
        /// Anything fleshy. These get Plugin.ThickestIce instead of regular ice.
        /// </summary>
        static readonly string[] fleshNames = { "mouth", "flesh", "vein", "minced", "faces" };

        public static void Init()
        {
            snow = Addressables.LoadAssetAsync<Material>("Assets/Materials/Snow.mat").WaitForCompletion(); // Load the snow
            ice = Addressables.LoadAssetAsync<Material>("Assets/Materials/IceThicker.mat").WaitForCompletion(); // Load the ice
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Plugin.Coroutine(delayedReplace());
        }

        static IEnumerator delayedReplace()
        {
            yield return new WaitForSeconds(0.25f);
            Replace();
        }

        public static void Replace()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                ReplaceObjectMaterial(obj);
            }
            OnFinish?.Invoke();
        }

        static bool NameContainsAny(string name, string[] keywords)
        {
            if (string.IsNullOrEmpty(name)) return false;

            string lower = name.ToLower();
            foreach (var keyword in keywords)
            {
                if (lower.Contains(keyword)) return true;
            }
            return false;
        }

        static bool isWaterName(string name) => NameContainsAny(name, waterNames);

        static bool isFleshName(string name) => NameContainsAny(name, fleshNames);

        public static bool isWater(GameObject obj)
        {
            if (obj == null) return false;

            if (isWaterName(obj.name)) return true;

            return obj.transform.parent != null
                && isWaterName(obj.transform.parent.gameObject.name);
        }

        public static bool isWater(Material mat)
        {
            if (mat == null) return false;

            if (isWaterName(mat.name)) return true;

            return false;
        }

        public static bool isWater(Renderer rend)
        {
            if (rend == null) return false;

            foreach (var mat in rend.sharedMaterials)
            {
                if (isWater(mat)) return true;
            }
            return false;
        }

        public static bool isFlesh(GameObject obj)
        {
            if (obj == null) return false;

            if (isFleshName(obj.name)) return true;

            return obj.transform.parent != null
                && isFleshName(obj.transform.parent.gameObject.name);
        }

        public static bool isFlesh(Material mat)
        {
            if (mat == null) return false;

            return isFleshName(mat.name);
        }

        public static void ReplaceObjectMaterial(GameObject obj)
        {
            if (Plugin.IgnoreObject(obj)) return;
            Renderer rend = obj.GetComponent<Renderer>();
            obj.isStatic = false; // yeh

            if (rend is ParticleSystemRenderer) return; // dont affect particles

            bool water = isWater(obj);
            bool flesh = isFlesh(obj);

            if (water || isWater(rend))
            {
                if (obj.TryGetComponent<ScrollingTexture>(out var tex)) Object.Destroy(tex);
            }

            ReplaceMaterial(rend, water, flesh);
        }

        const string SurfaceTypeProp = "_SurfaceType";

        const int SnowSurfaceType = 0;

        static void SetSurfaceTypeSnow(Material mat)
        {
            if (mat != null && mat.HasProperty(SurfaceTypeProp))
                mat.SetFloat(SurfaceTypeProp, SnowSurfaceType);
        }

        public static void ReplaceMaterial(Renderer rend, bool water = false, bool flesh = false)
        {
            if (rend == null || snow == null) return;

            var original = rend.sharedMaterials;
            var mats = new Material[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] == null) continue; // leaves null, which was already null, so fine

                // Fleshy stuff becomes the thickest ice, if it exists
                if (Plugin.ThickestIce != null && (flesh || isFlesh(original[i])))
                {
                    mats[i] = Plugin.ThickestIce;
                    continue;
                }

                if (water || isWater(original[i]))
                {
                    mats[i] = ice;
                    continue;
                }

                if (original[i].name.Contains("Grass"))
                {
                    original[i].color = Color.white;
                    mats[i] = original[i];
                    continue;
                }

                mats[i] = new Material(original[i]);
                ChangeTexture(mats[i]);
                SetSurfaceTypeSnow(mats[i]);
            }
            rend.sharedMaterials = mats;
        }

        public static void ChangeTexture(Material mat)
        {
            mat.mainTexture = snow.mainTexture;
        }
    }
}
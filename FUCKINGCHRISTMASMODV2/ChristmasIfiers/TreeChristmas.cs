using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FUCKINGCHRISTMASMOD.ChristmasIfiers
{
    public class TreeChristmas
    {
        public static void Init()
        {
            MaterialSwapper.OnFinish += ReplaceAllTrees;
        }
        private static void ReplaceAllTrees()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                string lower = obj.name.ToLower();
                if (lower.Contains("tree"))
                {
                    if (lower.Contains("christmas")) continue;
                    if (lower.Contains("street")) continue;
                    if (lower.Contains("hungry")) continue;
                    if(obj.name == "Trees" && (obj.transform.parent && obj.transform.parent.gameObject.name == "LimboSkybox"))
                    {
                        ReplaceLimboTrees(obj);
                    }
                    else
                    {
                        if (lower.Contains("trees")) continue; //trees prob means a collection lol
                        ReplaceTree(obj);
                    }

                }
            }
        }

        public static void ReplaceLimboTrees(GameObject parent)
        {
            var children = new List<Transform>();
            foreach (Transform t in parent.transform)
                children.Add(t);

            foreach (Transform original in children)
            {
                GameObject christmas = Object.Instantiate(Plugin.christmasTree, original.position, Plugin.christmasTree.transform.rotation, parent.transform);
                christmas.transform.localScale = Vector3.one * 0.2f;
                Object.Destroy(original.gameObject);
            }
        }

        public static void ReplaceTree(GameObject tree)
        {
            Transform original = tree.transform;
            GameObject christmas = Object.Instantiate(Plugin.christmasTree, original.position, original.rotation);
            christmas.transform.parent = original.parent;
            //christmas.transform.localScale = original.localScale;

            Object.Destroy(tree);
        }
    }
}

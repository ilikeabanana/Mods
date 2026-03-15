using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NewBananaWeapons
{
    // Token: 0x02000006 RID: 6
    public static class ShaderManager
    {
        static bool LoadedShaders = false;
        // Token: 0x06000018 RID: 24 RVA: 0x000028E2 File Offset: 0x00000AE2
        public static IEnumerator LoadShadersAsync()
        {
            AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync();
            while (!handle.IsDone)
            {
                yield return null;
            }
            bool flag = handle.Status == AsyncOperationStatus.Succeeded;
            if (flag)
            {
                IResourceLocator result = handle.Result;
                foreach (object obj in ((ResourceLocationMap)result).Keys)
                {
                    string text = (string)obj;
                    bool flag2 = text.EndsWith(".shader");
                    if (flag2)
                    {
                        AsyncOperationHandle<Shader> shaderHandle = Addressables.LoadAssetAsync<Shader>(text);
                        while (!shaderHandle.IsDone)
                        {
                            yield return null;
                        }
                        bool flag3 = shaderHandle.Status == AsyncOperationStatus.Succeeded;
                        if (flag3)
                        {
                            Shader result2 = shaderHandle.Result;
                            bool flag4 = result2 != null && result2.name != "ULTRAKILL/PostProcessV2" && !ShaderManager.shaderDictionary.ContainsKey(result2.name);
                            if (flag4)
                            {
                                ShaderManager.shaderDictionary[result2.name] = result2;
                            }
                            result2 = null;
                        }
                        else
                        {
                            string str = "Failed to load shader: ";
                            Exception operationException = shaderHandle.OperationException;
                            Debug.LogError(str + ((operationException != null) ? operationException.ToString() : null));
                            str = null;
                            operationException = null;
                        }
                        shaderHandle = default(AsyncOperationHandle<Shader>);
                        shaderHandle = default(AsyncOperationHandle<Shader>);
                    }
                    text = null;
                    //obj = null;
                }
                LoadedShaders = true;
                IEnumerator<object> enumerator = null;
                result = null;
            }
            else
            {
                string str2 = "Addressables initialization failed: ";
                Exception operationException2 = handle.OperationException;
                Debug.LogError(str2 + ((operationException2 != null) ? operationException2.ToString() : null));
                str2 = null;
                operationException2 = null;
            }
            yield break;
            yield break;
        }

        // Token: 0x06000019 RID: 25 RVA: 0x000028EC File Offset: 0x00000AEC
        public static string ModPath()
        {
            return Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf(Path.DirectorySeparatorChar));
        }

        // Token: 0x0600001A RID: 26 RVA: 0x00002922 File Offset: 0x00000B22
        public static IEnumerator ApplyShadersAsync(GameObject[] allGameObjects)
        {
            yield return new WaitUntil(() => LoadedShaders);
            bool flag = allGameObjects == null;
            if (flag)
            {
                yield break;
            }
            foreach (GameObject gameObject in allGameObjects)
            {
                bool flag2 = !(gameObject == null);
                if (flag2)
                {
                    foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                    {
                        bool flag3 = !(renderer == null);
                        if (flag3)
                        {
                            Material[] array2 = new Material[renderer.sharedMaterials.Length];
                            int num;
                            for (int i = 0; i < renderer.sharedMaterials.Length; i = num + 1)
                            {
                                Material material = renderer.sharedMaterials[i];
                                array2[i] = material;
                                Shader shader = null;
                                bool flag4 = !(material == null) && !(material.shader == null) && !ShaderManager.modifiedMaterials.Contains(material) && !(material.shader.name == "ULTRAKILL/PostProcessV2") && ShaderManager.shaderDictionary.TryGetValue(material.shader.name, out shader);
                                if (flag4)
                                {
                                    array2[i].shader = shader;
                                    ShaderManager.modifiedMaterials.Add(material);
                                }
                                material = null;
                                shader = null;
                                num = i;
                            }
                            renderer.materials = array2;
                            array2 = null;
                        }
                        //renderer = null;
                    }
                    Renderer[] array4 = null;
                    yield return null;
                }
                //gameObject = null;
            }
            GameObject[] array3 = null;
            yield break;
        }

        // Token: 0x0600001B RID: 27 RVA: 0x00002931 File Offset: 0x00000B31
        public static IEnumerator ApplyShaderToGameObject(GameObject gameObject)
        {
            yield return new WaitUntil(() => LoadedShaders);

            if (gameObject == null)
                yield break;

            foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var shared = renderer.sharedMaterials;
                Material[] newMats = null; // only allocate if needed
                bool changed = false;

                for (int i = 0; i < shared.Length; i++)
                {
                    Material mat = shared[i];
                    if (mat == null || mat.shader == null)
                        continue;

                    if (ShaderManager.modifiedMaterials.Contains(mat))
                        continue;

                    if (mat.shader.name == "ULTRAKILL/PostProcessV2")
                        continue;

                    if (ShaderManager.shaderDictionary.TryGetValue(mat.shader.name, out Shader replacement))
                    {
                        // First time we actually change something → clone array
                        if (!changed)
                        {
                            newMats = (Material[])shared.Clone();
                            changed = true;
                        }

                        newMats[i] = new Material(mat); // instance new material
                        newMats[i].shader = replacement;
                        ShaderManager.modifiedMaterials.Add(newMats[i]);
                    }
                }

                // Only apply if something changed
                if (changed)
                    renderer.materials = newMats;
            }

            yield return null;
        }


        // Token: 0x0400000C RID: 12
        public static Dictionary<string, Shader> shaderDictionary = new Dictionary<string, Shader>();

        // Token: 0x0400000D RID: 13
        private static HashSet<Material> modifiedMaterials = new HashSet<Material>();

        // Token: 0x0200000A RID: 10
        public class ShaderInfo
        {
            // Token: 0x17000007 RID: 7
            // (get) Token: 0x0600002A RID: 42 RVA: 0x00002A80 File Offset: 0x00000C80
            // (set) Token: 0x0600002B RID: 43 RVA: 0x00002A88 File Offset: 0x00000C88
            public string Name { get; set; }
        }
    }
}

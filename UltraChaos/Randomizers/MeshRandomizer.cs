using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ultrachaos.Randomizers
{
    public class MeshRandomizer : Randomizer<Mesh>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeMeshes.Value;
        }

        protected override int GetInstanceID(Mesh item)
        {
            return item.GetInstanceID();
        }
        private List<Mesh> cachedMeshes;
        public static MeshRandomizer Instance = new MeshRandomizer();
        public override void Initialize()
        {
            cachedMeshes = Resources.FindObjectsOfTypeAll<Mesh>().ToList();
            Plugin.OnInstantiateMethod.Add((obj) =>
            {
                foreach (var rend in obj.GetComponentsInChildren<MeshFilter>())
                {

                    ReplaceRenderer(rend, cachedMeshes);
                }
            });
        }
        public static void Init()
        {
            Plugin.Instance.StartCoroutine(Instance.Inititit());
        }
        public IEnumerator Inititit()
        {
            yield return new WaitForSeconds(0.1f);
            Instance.ChangeMeshes();
        }
        public void ChangeMeshes()
        {
            if (GetConfigValue() == RandomConfigValue.Disabled) return;
            MeshFilter[] meshes = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MeshFilter rend in meshes)
            {
                if (rend == null || rend.mesh == null) continue;
                if (rend.gameObject.layer == 19) continue;
                if (rend.GetComponent<ParticleSystem>() != null) continue;

                ReplaceRenderer(rend, cachedMeshes);
            }
        }

        public void ReplaceRenderer(MeshFilter rend, List<Mesh> pool)
        {
            try
            {
                if (rend == null) return;
                if (GetConfigValue() == RandomConfigValue.Disabled) return;

                rend.mesh = GetRandom(rend.mesh, pool);
            }
            catch (System.Exception) { }
        }
    }
}
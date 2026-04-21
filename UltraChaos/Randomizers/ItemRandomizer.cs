using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ultrachaos.Randomizers
{
    public class ItemRandomizer : Randomizer<ItemIdentifier>
    {
        protected override RandomConfigValue GetConfigValue()
        {
            return Plugin.ChangeItems.Value;
        }

        protected override int GetInstanceID(ItemIdentifier item)
        {
            if (item == null || item.gameObject == null)
                return 0;

            return Plugin.GetPrefabName(item.gameObject.name).GetHashCode();
        }

        public static ItemRandomizer Instance = new ItemRandomizer();

        public static IEnumerator ApplyChanges()
        {
            Instance.AddRangeToPool(Resources.FindObjectsOfTypeAll<ItemIdentifier>().Where((x) => x.gameObject.scene != SceneManager.GetActiveScene()));
            yield return new WaitForSeconds(1f);
            Instance.ChangeMats();
            yield return new WaitForSeconds(0.1f);
            Instance.ChangeMats();
        }

        public void ChangeMats()
        {
            if (GetConfigValue() == RandomConfigValue.Disabled) return;


            ReplaceItems();
        }

        private void ReplaceItems()
        {
            bool flag = GetConfigValue() == RandomConfigValue.Disabled;
            if (!flag)
            {
                List<ItemIdentifier> list = GameObject.FindObjectsByType<ItemIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
                foreach (ItemIdentifier itemIdentifier2 in list)
                {
                    ItemIdentifier itemIdentifier3 = Object.Instantiate<ItemIdentifier>(GetRandom(itemIdentifier2), itemIdentifier2.transform.position, itemIdentifier2.transform.rotation);
                    itemIdentifier3.pickedUp = itemIdentifier2.pickedUp;
                    itemIdentifier3.infiniteSource = itemIdentifier2.infiniteSource;
                    itemIdentifier3.onPickUp = itemIdentifier2.onPickUp;
                    itemIdentifier3.onPutDown = itemIdentifier2.onPutDown;
                    itemIdentifier3.ipz = itemIdentifier2.ipz;
                    itemIdentifier3.itemType = itemIdentifier2.itemType;
                    itemIdentifier3.transform.parent = itemIdentifier2.transform.parent;
                    itemIdentifier3.gameObject.SetActive(itemIdentifier2.gameObject.activeSelf);
                    Object.Destroy(itemIdentifier2.gameObject);
                }
            }
        }

    }
}
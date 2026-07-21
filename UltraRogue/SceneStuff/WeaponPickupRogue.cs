using System;
using TMPro;
using Ultrarogue.Characters;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Ultrarogue.Plugin;
using Random = UnityEngine.Random;

namespace Ultrarogue.SceneStuff
{
    public class WeaponPickupRogue : MonoBehaviour
    {
        public AWeapon weapon;
        bool pickedUp = false;
        Func<bool> canPickup;

        float messageCooldown = 0f; // for shop "not enough gold" spam prevention

        void Update()
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // because quad faces +Z

            if (messageCooldown > 0f)
                messageCooldown -= Time.deltaTime;

            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
            {
                if (pickedUp) return;
                if (canPickup != null)
                {
                    if (!canPickup.Invoke()) return;
                }
                pickedUp = true;
                Plugin.AddWeapon(weapon);
                Destroy(gameObject);
            }
        }

        // Returns true if the current character has the passive that makes all shop items purchasable.
        static bool HasShoppingPassive()
        {
            return Plugin.SelectedChar != null && Plugin.SelectedChar.HasPassive(Passive.Greedy);
        }

        public static GameObject ShopItemPrefab;

        static void AddShopPrefab(WeaponPickupRogue pickup, float offset)
        {
            if (ShopItemPrefab == null)
            {
                Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/ShopItemPrefab.prefab").Completed += handle =>
                {
                    ShopItemPrefab = handle.Result;
                };
                return; // prefab not ready yet this call, bail out
            }

            AWeapon chosenWeapon = pickup.weapon;
            int weaponBaseCost = 5;

            weaponBaseCost += RogueDifficultyManager.ItemRNG.Next(-1, 3);

            weaponBaseCost = Mathf.Max(4, weaponBaseCost);

            int price = ShopItem.GetScaledCost(weaponBaseCost);

            GameObject priceThingy = Instantiate(ShopItemPrefab, null);
            TMP_Text priceText = priceThingy.GetComponentInChildren<TMP_Text>();
            priceText.text = $"${price}";
            priceText.transform.position = pickup.transform.position + new Vector3(0, offset, 0);
            priceThingy.transform.parent = pickup.transform.parent;
            priceThingy.SetActive(true);
            Func<bool> existingCondition = pickup.canPickup;

            pickup.canPickup = () =>
            {
                if (existingCondition != null && !existingCondition.Invoke())
                    return false;

                var mgr = RogueDifficultyManager.Instance;
                if (mgr == null) return false;

                if (mgr.Gold >= price)
                {
                    mgr.Gold -= price;
                    HudMessageReceiver.Instance?.SendHudMessage($"Bought: {chosenWeapon}  (-{price} gold)");
                    Destroy(priceThingy);
                    return true;
                }
                else if (pickup.messageCooldown <= 0f)
                {
                    HudMessageReceiver.Instance?.SendHudMessage(
                        $"Need {price} gold  (you have {mgr.Gold})");
                    pickup.messageCooldown = 2f;
                }
                return false;
            };
        }

        public static void CreatePickup(Transform position)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickup.GetComponent<Collider>().enabled = false;

            AWeapon weapon = AWeapon.GenerateWeapon();
            Material mat = new Material(AssetsManager.weaponMat);
            mat.SetInt("_CullMode", 0); // Off
            mat.EnableKeyword("BILLBOARD");

            Sprite icon = AssetsManager.prefToSprite(weapon.ToString(), weapon.Alternate);
            Rect rect = icon.textureRect;
            Texture2D atlas = icon.texture;

            // Extract sprite region into a RenderTexture
            RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);
            Graphics.Blit(atlas, rt,
                new Vector2(rect.width / atlas.width, rect.height / atlas.height),
                new Vector2(rect.x / atlas.width, rect.y / atlas.height));

            // Read it into a CPU-readable Texture2D
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D((int)rect.width, (int)rect.height);
            tex.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();

            // Invert RGB (black -> white), preserve alpha
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(1 - pixels[i].r, 1 - pixels[i].g, 1 - pixels[i].b, pixels[i].a);
            tex.SetPixels(pixels);
            tex.Apply();

            mat.mainTexture = tex;

            pickup.GetComponent<MeshRenderer>().material = mat;

            WeaponPickupRogue p = pickup.AddComponent<WeaponPickupRogue>();
            p.weapon = weapon;
            pickup.transform.position = position.position + Vector3.up * 2;
            pickup.transform.parent = position;
            pickup.transform.localScale *= 2;
            pickup.transform.localScale = new Vector3(
                pickup.transform.localScale.x * 2f,
                pickup.transform.localScale.y,
                pickup.transform.localScale.z
            );

            if (HasShoppingPassive())
                AddShopPrefab(p, 2f);
        }

        public static void CreatePickupConditional(Transform position, Func<bool> pickupCon, float offset = 2, AWeapon weapon = null, bool isShop = false)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickup.GetComponent<Collider>().enabled = false;
            if (weapon == null)
                weapon = AWeapon.GenerateWeapon();
            Material mat = new Material(AssetsManager.weaponMat);
            mat.SetInt("_CullMode", 0); // Off
            mat.EnableKeyword("BILLBOARD");

            Sprite icon = AssetsManager.prefToSprite(weapon.ToString(), weapon.Alternate);
            Rect rect = icon.textureRect;
            Texture2D atlas = icon.texture;

            // Extract sprite region into a RenderTexture
            RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);
            Graphics.Blit(atlas, rt,
                new Vector2(rect.width / atlas.width, rect.height / atlas.height),
                new Vector2(rect.x / atlas.width, rect.y / atlas.height));

            // Read it into a CPU-readable Texture2D
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D((int)rect.width, (int)rect.height);
            tex.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();

            // Invert RGB (black -> white), preserve alpha
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(1 - pixels[i].r, 1 - pixels[i].g, 1 - pixels[i].b, pixels[i].a);
            tex.SetPixels(pixels);
            tex.Apply();

            mat.mainTexture = tex;
            pickup.GetComponent<MeshRenderer>().material = mat;
            WeaponPickupRogue pickup_component = pickup.AddComponent<WeaponPickupRogue>();
            pickup_component.weapon = weapon;
            pickup_component.canPickup = pickupCon;
            pickup.transform.position = position.position + Vector3.up * offset;
            pickup.transform.parent = position;
            pickup.transform.localScale *= 2;
            pickup.transform.localScale = new Vector3(
                pickup.transform.localScale.x * 2f,
                pickup.transform.localScale.y,
                pickup.transform.localScale.z
            );

            if (HasShoppingPassive() && !isShop)
                AddShopPrefab(pickup_component, offset);
        }
    }
}
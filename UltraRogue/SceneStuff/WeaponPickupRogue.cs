using System;
using UnityEngine;
using static Ultrarogue.Plugin;
using Random = UnityEngine.Random;

namespace Ultrarogue.SceneStuff
{
    public class WeaponPickupRogue : MonoBehaviour
    {
        public AWeapon weapon;
        bool pickedUp = false;
        Func<bool> canPickup;

        void Update()
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // because quad faces +Z

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

            pickup.AddComponent<WeaponPickupRogue>().weapon = weapon;
            pickup.transform.position = position.position + Vector3.up * 2;
            pickup.transform.parent = position;
            pickup.transform.localScale *= 2;
            pickup.transform.localScale = new Vector3(
                pickup.transform.localScale.x * 2f,
                pickup.transform.localScale.y,
                pickup.transform.localScale.z
            );
        }

        public static void CreatePickupConditional(Transform position, Func<bool> pickupCon, float offset = 2, AWeapon weapon = null)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickup.GetComponent<Collider>().enabled = false;
            if(weapon == null)
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
        }
    }
}
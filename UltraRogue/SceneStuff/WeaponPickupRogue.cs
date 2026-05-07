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

            mat.mainTexture = AssetsManager.prefToDescriptor(weapon.ToString(), weapon.Alternate).icon.texture;
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

        public static void CreatePickupConditional(Transform position, Func<bool> pickupCon)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickup.GetComponent<Collider>().enabled = false;

            AWeapon weapon = AWeapon.GenerateWeapon();
            Material mat = new Material(AssetsManager.weaponMat);
            mat.SetInt("_CullMode", 0); // Off
            mat.EnableKeyword("BILLBOARD");

            mat.mainTexture = AssetsManager.prefToDescriptor(weapon.ToString(), weapon.Alternate).icon.texture;
            pickup.GetComponent<MeshRenderer>().material = mat;
            WeaponPickupRogue pickup_component = pickup.AddComponent<WeaponPickupRogue>();
            pickup_component.weapon = weapon;
            pickup_component.canPickup = pickupCon;
            pickup.transform.position = position.position + Vector3.up * 2;
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
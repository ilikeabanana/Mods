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
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
            {
                if (pickedUp) return;
                if (canPickup != null)
                {
                    if (!canPickup.Invoke()) return;
                }
                pickedUp = true;
                HudMessageReceiver.Instance?.SendHudMessage(weapon.ToString());
                Plugin.weapons.Add(weapon);
                if (weapon.weapon == Weapon.Arm)
                    FistControl.Instance.ResetFists();
                else
                    GunSetter.Instance.ResetWeapons();
                Destroy(gameObject);
            }
        }

        public static void CreatePickup(Vector3 position)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pickup.GetComponent<Collider>().enabled = false;

            Weapon weaponEnum = (Weapon)Random.Range(0, System.Enum.GetValues(typeof(Weapon)).Length);
            Variant variantEnum = (Variant)Random.Range(0, System.Enum.GetValues(typeof(Variant)).Length);

            AWeapon weapon = new AWeapon(weaponEnum, variantEnum);

            pickup.AddComponent<WeaponPickupRogue>().weapon = weapon;
            pickup.transform.position = position + Vector3.up * 2;
        }

        public static void CreatePickupConditional(Vector3 position, Func<bool> pickupCon)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pickup.GetComponent<Collider>().enabled = false;

            Weapon weaponEnum = (Weapon)Random.Range(0, System.Enum.GetValues(typeof(Weapon)).Length);
            Variant variantEnum = (Variant)Random.Range(0, System.Enum.GetValues(typeof(Variant)).Length);

            AWeapon weapon = new AWeapon(weaponEnum, variantEnum);

            WeaponPickupRogue pickup_component = pickup.AddComponent<WeaponPickupRogue>();
            pickup_component.weapon = weapon;
            pickup_component.canPickup = pickupCon;
            pickup.transform.position = position + Vector3.up * 2;
        }
    }
}
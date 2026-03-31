using UnityEngine;
using static Ultrarogue.Plugin;

namespace Ultrarogue.SceneStuff
{
    public class WeaponPickupRogue : MonoBehaviour
    {
        public AWeapon weapon;
        bool pickedUp = false;
        void Update()
        {
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f)
            {
                if (pickedUp) return;
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
    }
}

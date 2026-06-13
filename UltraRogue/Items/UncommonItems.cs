using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Enemy;
using Ultrarogue.Characters;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ultrarogue.Items
{
    // All uncommon items either:
    //   a) use DamageModifier / DeathEffect / HitEffect which already gate on GetItemCount > 0, or
    //   b) have no persistent state that survives item removal.
    // None require an OnRemoval override.

    public class IgnitionTank : BaseItem
    {
        public override string ItemName => "Ignition Tank";
        public override string itemDescription => "Fire deals 100% more damage";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int itemCount = Plugin.GetItemCount(this);
                if (eid.hitter == "fire")
                {
                    return itemCount + 1;
                }

                return 1;
            });
        }
    }
    public class RationCard : BaseItem
    {
        public override string ItemName => "Ration Card";
        public override string itemDescription => "Unlock a new area in the shop...";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override bool CanOnlyHaveOne => true;
    }

    public class Panopticon : BaseItem
    {
        public override Rarity Rarity => Rarity.Uncommon;
        public override string ItemName => "Panopticon";
        public override string itemDescription => "On damage taken, heal 5 (+5 per stack) hp";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing, ItemTag.Health };

        public override void OnStart()
        {
            new DamageTakenEffect(ItemName, (dmg) =>
            {
                int c = Plugin.GetItemCount(this);

                if (c <= 0) return;

                NewMovement.Instance.GetHealth(5 * c, true, bloodsplatter: false);
            });
        }
    }


    public class Executioner : BaseItem
    {
        public override string ItemName => "Executioner";
        public override string itemDescription => "Enemies below 20% HP take 100% more damage (+100% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return 1f;

                float hpPercent = eid.health / eid.GetComponent<Enemy>().originalHealth;
                if (hpPercent < 0.20f)
                    return 1f + (1.0f * count);

                return 1f;
            });
        }
    }

    public class Fusion : BaseItem
    {
        public override string ItemName => "Fusion";
        public override string itemDescription => "Each kill permanently increases your max hp by 1 with a max of 50 (+50 per stack)";
        public override Rarity Rarity => Ultrarogue.Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.MaxHealth, ItemTag.Health };
        Change hpChange;
        float killBonus = 0f;

        public override void OnStart()
        {
            hpChange = new Change(addition: 0);
            new PlayerChange(maxHealth: hpChange);

            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                if (killBonus >= 50 * count) return;
                killBonus += 1;
                if(Plugin.SelectedChar.GetType() != typeof(Filth))
                    NewMovement.Instance.GetHealth(1, true);
            });
        }

        public override void OnUpdate(int count)
        {
            hpChange.addition = killBonus;
        }

        public override void OnRemoval()
        {
            // Reset the accumulated kill bonus so it doesn't carry over if re-acquired
            killBonus = 0f;
            hpChange.addition = 0;
        }
    }

    public class Combatblood : BaseItem
    {
        public override string ItemName => "Combat blood";
        public override string itemDescription => "On kill, restore 6 HP (+6 per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing, ItemTag.Health };
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return;

                int heal = 6 * count;
                NewMovement.Instance.hp = Mathf.Min(NewMovement.Instance.hp + heal, Plugin.MaxHealth);
            });
        }
    }

    public class WillOWisp : BaseItem
    {
        public override string ItemName => "Will-o'-the-Maurice";
        public override string itemDescription => "On kill, 35% chance to detonate the corpse for 350% damage in a 6m radius (+6m and +350% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override string ItemIconName => "WillOMaurice";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                if (!Plugin.canExecute(35f, "")) return;
                Plugin.Instance.StartCoroutine(explody(eid.transform.position));
            });
        }

        IEnumerator explody(Vector3 position)
        {
            yield return new WaitForSeconds(0.25f);
            int count = Plugin.GetItemCount(this);

            float radius = 6f * count;
            float damage = 3.5f * count;

            GameObject explosion = Object.Instantiate(DefaultReferenceManager.Instance.explosion, position, Quaternion.identity);
            foreach (var exp in explosion.GetComponentsInChildren<Explosion>())
            {
                exp.maxSize = radius;
                exp.canHit = AffectedSubjects.EnemiesOnly;
                exp.damage = Mathf.RoundToInt(damage);
            }
        }
    }

    [HarmonyPatch]
    public class Bouncy : BaseItem
    {
        public override string ItemName => "Bouncy Hitscans";
        public override string itemDescription => "All hitscans have a 25% (+15% chance per stack) to bounce (chance gets smaller every bounce)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        public static void Prefix(RevolverBeam __instance)
        {
            int count = Plugin.GetItemCount("Bouncy Hitscans");
            if (count <= 0) return;
            if (__instance.hasBeenRicocheter) return;
            if (__instance.beamType == BeamType.Enemy) return;
            if (__instance.beamType == BeamType.MaliciousFace) return;
            float baseChance = 25f + (15f * (count - 1));

            float chance = baseChance;

            while (Plugin.canExecute(chance, ""))
            {
                __instance.ricochetAmount++;
                if (__instance.hitAmount < 2) __instance.hitAmount = 2;
                chance /= 2;
            }
        }
    }

    public class DeadMansHand : BaseItem
    {
        public override string ItemName => "Dead Man's Hand";
        public override string itemDescription => "Below 25% HP, deal 75% more damage (+75% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return 1f;

                float hpPercent = (float)NewMovement.Instance.hp / Plugin.MaxHealth;
                if (hpPercent < 0.25f)
                    return 1f + (0.75f * count);

                return 1f;
            });
        }
    }

    public class NailBomb : BaseItem
    {
        public override string ItemName => "Nail Bomb";
        public override string itemDescription => "Nailgun kills explode for 150% damage in a 5m radius (+5m per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Nailgun };
        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;

                List<string> nailHitters = Plugin.WeaponToHitter(Plugin.Weapon.Nailgun);
                if (!nailHitters.Contains(eid.hitter)) return;

                Plugin.Instance.StartCoroutine(NailExplosion(eid.transform.position, count));
            });
        }

        IEnumerator NailExplosion(Vector3 position, int count)
        {
            yield return new WaitForEndOfFrame();

            GameObject explosion = Object.Instantiate(
                DefaultReferenceManager.Instance.explosion,
                position,
                Quaternion.identity
            );
            foreach (var exp in explosion.GetComponentsInChildren<Explosion>())
            {
                exp.maxSize = 5f * count;
                exp.canHit = AffectedSubjects.EnemiesOnly;
                exp.damage = Mathf.RoundToInt(1.5f * count);
            }
        }
    }

    public class SpikyNails : BaseItem
    {
        public override string ItemName => "Spiky Nails";
        public override string itemDescription => "Enemies with nail get +0.1% (+0.1% per stack) more damage per nail stuck in them.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Nailgun };
        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return 1f;

                int nailsStuck = eid.nailsAmount;
                float damageIncrease = (0.001f * count) * nailsStuck;
                return damageIncrease + 1;
            });
        }
    }

    public class MissleLauncher : BaseItem
    {
        public override string ItemName => "Missle Launcher";
        public override string itemDescription => "10% chance to launch a missile that deals 300% (+300% per stack) base damage to an enemy";

        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Uncommon;

        bool attempted = false;
        GameObject missleModel = null;


        GameObject getMissleModel()
        {
            if (!attempted)
            {
                attempted = true;
                missleModel = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Missile.prefab").WaitForCompletion();
            }

            if (missleModel != null)
            {
                GameObject missle = GameObject.Instantiate(missleModel);
                return missle;
            }

            // fallback
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.GetComponent<Collider>().isTrigger = true;
            fallback.AddComponent<Rigidbody>().useGravity = false;
            return fallback;
        }

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                if (!Plugin.canExecute(10, eid.hitter)) return;

                float damage = (3 * count);
                GameObject missle = getMissleModel();
                Missle proj = missle.AddComponent<Missle>();
                proj.damage = damage;
                proj.enemyThatGotHit = eid;
                missle.transform.position = CameraController.Instance.GetDefaultPos() + Vector3.up * 3.5f;
            });
        }
    }
}
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Ultrarogue.Items
{
    public class FiringFaster : BaseItem
    {
        public override string ItemName => "Firing";
        public override string itemDescription => "Increase firerate by 15%";
        Change atkSpeedChange;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override void OnStart()
        {
            atkSpeedChange = new Change(percentage: 0);
            new PlayerChange(attackSpeed: atkSpeedChange);
        }

        public override void OnUpdate(int count)
        {
            atkSpeedChange.percentage = 0.15f * count;
        }
    }

    [HarmonyPatch]
    public class BiggerShells : BaseItem
    {
        static BiggerShells Instance { get; set; }
        public override string ItemName => "Bigger Shells";
        public override string itemDescription => "Shotgun damage +10%, projectiles are 7% larger";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override void OnStart()
        {
            Instance = this;
            damageChange = new DamageChange(Plugin.Weapon.Shotgun, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.10f * (float)count;
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Awake))]
        public static void Prefix(Projectile __instance)
        {
            if (Plugin.GetItemCount(Instance) > 0 && __instance.playerBullet)
            {
                __instance.transform.localScale *= 1 + (0.07f * Plugin.GetItemCount(Instance));
            }
        }
    }

    public class Gasoline : BaseItem
    {
        public override string ItemName => "Gasoline";
        public override string itemDescription => "On kill, ignite nearby enemies in a 5m radius (+5m per stack) for 120% damage";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                Plugin.Instance.StartCoroutine(delayedExplody(eid));
            });
        }

        IEnumerator delayedExplody(EnemyIdentifier eid)
        {
            yield return new WaitForEndOfFrame();

            int count = Plugin.GetItemCount(this);

            float radius = 5f * count;
            Collider[] hits = Physics.OverlapSphere(eid.transform.position, radius);

            foreach (Collider col in hits)
            {
                EnemyIdentifier? nearby = col.GetComponent<EnemyIdentifier>();
                if (nearby == null || nearby == eid || nearby.dead) continue;

                nearby.hitter = "fire";
                nearby.DeliverDamage(
                    col.gameObject,
                    Vector3.zero,
                    eid.transform.position,
                    multiplier: 0.5f,
                    false
                );
                if (nearby.flammables != null && nearby.flammables.Count > 0)
                {
                    nearby.StartBurning((float)(15 / 10));
                }
                else
                {
                    Flammable componentInChildren = nearby.GetComponentInChildren<Flammable>();
                    if (componentInChildren != null)
                    {
                        componentInChildren.Burn((float)(15 / 10), false);
                    }
                }
            }
        }
    }

    public class KnuckleDuster : BaseItem
    {
        public override string ItemName => "Knuckle Duster";
        public override string itemDescription => "Arm damage +15%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;

        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Arm, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.15f * count;
        }
    }

    public class SpeedLoader : BaseItem
    {
        public override string ItemName => "Speed Loader";
        public override string itemDescription => "Revolver damage +12%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;

        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Revolver, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.12f * count;
        }
    }
    public class RunningShoes : BaseItem
    {
        public override string ItemName => "Running Shoes";
        public override string itemDescription => "Move speed +8%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change moveChange;

        public override void OnStart()
        {
            moveChange = new Change(percentage: 0);
            new PlayerChange(moveSpeed: moveChange);
        }

        public override void OnUpdate(int count)
        {
            moveChange.percentage = 0.08f * count;
        }
    }

    public class APRounds : BaseItem
    {
        public override string ItemName => "AP Rounds";
        public override string itemDescription => "Railcannon damage +15%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;

        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Railcannon, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.15f * count;
        }
    }

    public class LooseNails : BaseItem
    {
        public override string ItemName => "Loose Nails";
        public override string itemDescription => "Nailgun damage +10%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;

        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Nailgun, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.10f * count;
        }
    }
    public class PogoStick : BaseItem
    {
        public override string ItemName => "Pogo Stick";
        public override string itemDescription => "Jump height +15%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change jumpChange;

        public override void OnStart()
        {
            jumpChange = new Change(percentage: 0);
            new PlayerChange(jumpHeight: jumpChange);
        }

        public override void OnUpdate(int count)
        {
            jumpChange.percentage = 0.15f * count;
        }
    }

    public class Combatblood : BaseItem
    {
        public override string ItemName => "Combat blood";
        public override string itemDescription => "On kill, restore 3 HP (+1 per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return;

                int heal = 2 + count;
                NewMovement.Instance.hp = Mathf.Min(NewMovement.Instance.hp + heal, Plugin.MaxHealth);
            });
        }
    }

    public class IronSights : BaseItem
    {
        public override string ItemName => "Iron Sights";
        public override string itemDescription => "Global damage +6%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change dmgChange;

        public override void OnStart()
        {
            dmgChange = new Change(percentage: 0);
            new PlayerChange(globalDamageMult: dmgChange);
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = 0.06f * count;
        }
    }

    public class GuttertankHand : BaseItem
    {
        public override string ItemName => "Gutter tank Hand";
        public override string itemDescription => "Rocket Launcher damage +12%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;

        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.RocketLauncher, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.12f * count;
        }
    }
}
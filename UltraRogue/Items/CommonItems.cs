using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ULTRAKILL.Portal;
using ULTRAKILL.Portal.Native;
using Ultrarogue.Characters;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Ultrarogue.Items
{
    public class SoldierChip : BaseItem
    {
        public override string ItemName => "Soldier Chip";
        public override string itemDescription => "Increase firerate by 20%";
        Change atkSpeedChange;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            atkSpeedChange = new Change(percentage: 0);
            new PlayerChange(attackSpeed: atkSpeedChange);
        }

        public override void OnUpdate(int count)
        {
            atkSpeedChange.percentage = 0.20f * count;
        }

        public override void OnRemoval()
        {
            atkSpeedChange.percentage = 0;
        }
    }

    public class Monocle : BaseItem
    {
        public override string ItemName => "Monocle";
        public override string itemDescription => "Gain 2 (+2 per stack) gold when entering a new floor.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override void OnNewFloor(int count)
        {
            RogueDifficultyManager.Instance.Gold += 2 * count;
        }
    }

    public class StyleChst : BaseItem
    {
        public override string ItemName => "Style Chest";
        public override string itemDescription => "After gaining 10000 style remove this and spawn 1 random uncommon item.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        int styleStart = 0;
        public override void OnGotten(int count, bool firstPickup)
        {
            if (firstPickup)
                styleStart = StatsManager.Instance.stylePoints;
        }

        public override void OnUpdate(int count)
        {
            base.OnUpdate(count);
            if (!Room.isFighting) return;
            if (count == 0) return;
            int gainedStyle = StatsManager.Instance.stylePoints - styleStart;
            if (gainedStyle >= 10000)
            {
                styleStart = StatsManager.Instance.stylePoints;
                SpawnItem();
                Plugin.RemoveItem(this, 1);
            }
        }

        public void SpawnItem()
        {
            Transform playerTransform = NewMovement.Instance.transform;

            bool floorHit = Physics.Raycast(playerTransform.position, Vector3.down, out RaycastHit floorCheck, 40f, LayerMaskDefaults.Get(LMD.Environment));

            Vector3 spawnPosition = floorHit ? floorCheck.point : playerTransform.position;

            GameObject plc = new GameObject("ItemDropAnchor");
            plc.transform.position = spawnPosition;
            plc.transform.parent = Room.getObjectInsideRoom(spawnPosition).transform;
            // Spawn a random uncommon item
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.ChestRNG, DroptableType.UncommonOnly), plc.transform);

            if (Room.pedestalItem == null)
                Room.pedestalItem = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/Draghtnim/Pedestal.prefab").WaitForCompletion();
            Object.Instantiate(AssetsManager.spawnEffect, plc.transform.position, Quaternion.identity);
            if (Room.pedestalItem != null)
            {
                GameObject ped = Object.Instantiate(Room.pedestalItem, plc.transform.position + Vector3.up, Quaternion.identity);
                ped.transform.parent = Room.getObjectInsideRoom(spawnPosition).transform;
            }
        }
    }

    [HarmonyPatch]
    public class BiggerShells : BaseItem
    {
        static BiggerShells Instance { get; set; }
        public override string ItemName => "Bigger Shells";
        public override string itemDescription => "Shotgun damage +30%, projectiles are 12% larger";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Shotgun };
        DamageChange damageChange;
        public override void OnStart()
        {
            Instance = this;
            damageChange = new DamageChange(Plugin.Weapon.Shotgun, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.30f * (float)count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Awake))]
        public static void Prefix(Projectile __instance)
        {
            if (!Plugin.isInRogueScene()) return;
            if (Plugin.GetItemCount(Instance) > 0 && __instance.playerBullet)
            {
                __instance.transform.localScale *= 1 + (0.12f * Plugin.GetItemCount(Instance));
            }
        }
    }

    public class Improvement : BaseItem
    {
        public override string ItemName => "Scrap Parts";
        public override string itemDescription => "+10% to your lowest stat";
        PlayerChange plrChanges;
        public override void OnStart()
        {
            plrChanges = new PlayerChange();
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            if (NewMovement.Instance == null) return;
            if (Plugin.GetItemCount(FragileParts.I.ItemName) >= 0)
            {
                FragileParts.Reset();
            }
            // Movement speed
            float speed = NewMovement.Instance.walkSpeed;
            float baseSpeed = Plugin.Instance.normalMoveSpeed;

            float speedMult = speed / baseSpeed;

            // Attack speed
            float atkSpeed = Plugin.AttackSpeed.CalculateChanges(1f);

            // Damage
            float dmg = Plugin.globalDamageMult.CalculateChanges(1f);

            // Cooldown
            float cd = Plugin.cooldownReduction.CalculateChanges(1f);

            var stats = new[]
            {
                ("MS", speedMult),
                ("AS", atkSpeed),
                ("D", dmg),
                ("C", cd)
            };

            if (Plugin.SelectedChar is Filth)
            {
                stats = stats.Where(x => x.Item1 != "AS").ToArray();
            }

            var lowest = stats.OrderBy(x => x.Item2).First();

            if (lowest.Item1 == "MS")
                plrChanges.moveSpeed.percentage += 0.10f;
            else if (lowest.Item1 == "AS")
                plrChanges.attackSpeed.percentage += 0.10f;
            else if (lowest.Item1 == "D")
                plrChanges.globalDamageMult.percentage += 0.10f;
            else if (lowest.Item1 == "C")
                plrChanges.cooldownRed.percentage += 0.10f;
        }

        public override void OnRemoval()
        {
            plrChanges.moveSpeed = new Change();
            plrChanges.attackSpeed = new Change();
            plrChanges.globalDamageMult = new Change();
            plrChanges.cooldownRed = new Change();
        }


    }

    // Gasoline and SandWorm use DeathEffect/DamageModifier that already gate on
    // GetItemCount, so no OnRemoval is needed for them.

    public class Gasoline : BaseItem
    {
        public override string ItemName => "Gasoline";
        public override string itemDescription => "On kill, create 10 (+5 per stack) gasoline projectiles";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                for (int i = 0; i < (5 * count) + 10; i++)
                {
                    StartCoroutine(SpawnNapalm(eid.transform));
                }
            });
        }
        IEnumerator SpawnNapalm(Transform pos)
        {
            yield return new WaitForEndOfFrame();
            GameObject obj = UnityEngine.Object.Instantiate(
                AssetsManager.napalmProj,
                pos.position + Vector3.up,
                Quaternion.identity
            );

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;

            rb.velocity = randomDir * 25f;
        }
    }

    public class SmallKit : BaseItem
    {
        public override string ItemName => "Small Kit";
        public override string itemDescription => "+10 max hp per stack";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.MaxHealth, ItemTag.Health };

        Change hpChange;
        public override void OnStart()
        {
            hpChange = new Change();
            new PlayerChange(maxHealth: hpChange);
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            if (count == 0) return;
            NewMovement.Instance.GetHealth(10, true);
        }
        public override void OnUpdate(int count)
        {
            hpChange.addition = 10 * count;
        }
        public override void OnRemoval()
        {
            hpChange.addition = 0;
        }
    }
    public class SandWorm : BaseItem
    {
        public override string ItemName => "Sand Worm";
        public override string itemDescription => "Sanded enemies take +35% more damage";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c == 0) return 1;
                if (!eid.sandified) return 1f;
                return 1f + (0.35f * c);
            });
        }
    }

    public class KnuckleDuster : BaseItem
    {
        public override string ItemName => "Knuckle Duster";
        public override string itemDescription => "Arm damage +50%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Arm };
        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Arm, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.5f * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        } 
    }

    public class SpeedLoader : BaseItem
    {
        public override string ItemName => "Heavy Loader";
        public override string itemDescription => "Revolver damage +35%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };
        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Revolver, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.35f * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class RunningShoes : BaseItem
    {
        public override string ItemName => "Running Shoes";
        public override string itemDescription => "Move speed +20%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change moveChange;

        public override void OnStart()
        {
            moveChange = new Change(percentage: 0);
            new PlayerChange(moveSpeed: moveChange);
        }

        public override void OnUpdate(int count)
        {
            moveChange.percentage = 0.2f * count;
        }

        public override void OnRemoval()
        {
            moveChange.percentage = 0;
        }
    }

    public class APRounds : BaseItem
    {
        public override string ItemName => "AP Rounds";
        public override string itemDescription => "Railcannon damage +15%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Railcannon };
        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Railcannon, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.15f * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class LooseNails : BaseItem
    {
        public override string ItemName => "Loose Nails";
        public override string itemDescription => "Nailgun damage +25%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Nailgun };
        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.Nailgun, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.25f * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class PogoStick : BaseItem
    {
        public override string ItemName => "Pogo Stick";
        public override string itemDescription => "Jump Height +5% and slam damage +50%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change jumpChange;

        public override void OnStart()
        {
            jumpChange = new Change(percentage: 0);
            new PlayerChange(jumpHeight: jumpChange);

            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return 1f;

                if (eid.hitter == "ground slam") return 1f + (0.5f * count);

                return 1f;
            });
        }

        public override void OnUpdate(int count)
        {
            jumpChange.percentage = 0.05f * count;
        }

        public override void OnRemoval()
        {
            jumpChange.percentage = 0;
        }
    }

    public class IronSights : BaseItem
    {
        public override string ItemName => "Iron Sights";
        public override string itemDescription => "+12% damage per stack";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change dmgChange;

        public override void OnStart()
        {
            dmgChange = new Change(percentage: 0);
            new PlayerChange(globalDamageMult: dmgChange);
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = 0.12f * count;
        }

        public override void OnRemoval()
        {
            dmgChange.percentage = 0;
        }
    }

    public class GuttertankHand : BaseItem
    {
        public override string ItemName => "Gutter tank Hand";
        public override string itemDescription => "Rocket Launcher damage +22%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange damageChange;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.RocketLauncher };
        public override void OnStart()
        {
            damageChange = new DamageChange(Plugin.Weapon.RocketLauncher, new Change(percentage: 0));
            new PlayerChange(damageChanges: new List<DamageChange>() { damageChange });
        }

        public override void OnUpdate(int count)
        {
            damageChange.damageChange.percentage = 0.22f * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class Test : ActiveItem
    {
        public override string ItemName => "Damage Book";
        public override int ChargeRequired => 3;
        public override string itemDescription => "On activation, gain 50% damage temporarily (resets when exiting combat).";
        Change change = new Change();
        PlayerChange plr;
        public override void OnStart()
        {
            plr = new PlayerChange(globalDamageMult: change);
        }
        public override void OnUse()
        {
            change.percentage = 0.50f;
        }
        public override void OnUpdate(int count)
        {
            if (!Room.isFighting) change.percentage = 0;
        }
    }
}
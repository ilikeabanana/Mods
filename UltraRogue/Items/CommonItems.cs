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
        const float AttackSpeedPerStack = 0.20f;

        public override string ItemName => "Soldier Chip";
        public override string itemDescription => $"Increase firerate by {AttackSpeedPerStack * 100}%";
        Change atkSpeedChange;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            atkSpeedChange = new Change(percentage: 0);
            new PlayerChange(attackSpeed: atkSpeedChange);
        }

        public override void OnUpdate(int count)
        {
            atkSpeedChange.percentage = AttackSpeedPerStack * count;
        }

        public override void OnRemoval()
        {
            atkSpeedChange.percentage = 0;
        }
    }

    public class HitscanSlop : BaseItem
    {
        const float Chance = 25f;
        const float DamagePerStack = 0.5f;

        public override string ItemName => "Hitscan on hit";
        public override string itemDescription => $"{Chance}% chance on hit to fire a revolver beam to the nearest enemy dealing {DamagePerStack * 100}% (+{DamagePerStack * 100}% per stack) TOTAL damage";
        public override float SpawnWeight => 0.9f;
        public override List<Plugin.Weapon> WeaponProvisions => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };
        public override void OnStart()
        {
            base.OnStart();
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c <= 0) return;
                if (eid.hitter == ItemName) return;

                if (Plugin.canExecute(Chance, eid.hitter))
                {
                    GameObject beam = Object.Instantiate(AssetsManager.RevolverBeam, CameraController.Instance.GetDefaultPos(), Quaternion.identity);

                    RevolverBeam pew = beam.GetComponent<RevolverBeam>();
                    pew.hitterOverride = ItemName;
                    pew.damage = dmg * (DamagePerStack * c);
                    List<EnemyIdentifier> enemies = EnemyTracker.Instance.GetCurrentEnemies();

                    EnemyIdentifier nearest = null;
                    float closestSqrDist = float.MaxValue;

                    foreach (EnemyIdentifier enemy in enemies)
                    {
                        if (enemy == null)
                            continue;

                        if (enemy.dead)
                            continue;

                        float sqrDist = (enemy.transform.position - beam.transform.position).sqrMagnitude;

                        if (sqrDist < closestSqrDist)
                        {
                            closestSqrDist = sqrDist;
                            nearest = enemy;
                        }
                    }

                    if (nearest != null)
                    {
                        beam.transform.forward = (nearest.transform.position - beam.transform.position).normalized;
                    }

                }
            }, true);
        }
    }

    public class Monocle : BaseItem
    {
        const int GoldPerStack = 2;

        public override string ItemName => "Monocle";
        public override string itemDescription => $"Gain {GoldPerStack} (+{GoldPerStack} per stack) gold when entering a new floor.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override void OnNewFloor(int count)
        {
            RogueDifficultyManager.Instance.Gold += GoldPerStack * count;
        }
    }

    public class StyleChst : BaseItem
    {
        const int StyleRequired = 10000;

        public override string ItemName => "Style Chest";
        public override string itemDescription => $"After gaining {StyleRequired} style remove this and spawn 1 random uncommon item.";
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
            if (gainedStyle >= StyleRequired)
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
            ItemPickup.CreatePickup(Plugin.GiveRandomItem(RogueDifficultyManager.ChestRNG, DroptableType.UncommonOnly), plc.transform, delay: 2);

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
        const float DamagePerStack = 0.30f;
        const float SizePerStack = 0.12f;

        static BiggerShells Instance { get; set; }
        public override string ItemName => "Bigger Shells";
        public override string itemDescription => $"Shotgun damage +{DamagePerStack * 100}%, projectiles are {SizePerStack * 100}% larger";
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
            damageChange.damageChange.percentage = DamagePerStack * (float)count;
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
                __instance.transform.localScale *= 1 + (SizePerStack * Plugin.GetItemCount(Instance));
            }
        }
    }

    public class Improvement : BaseItem
    {
        const float LowestStatBonus = 0.10f;

        public override string ItemName => "Scrap Parts";
        public override string itemDescription => $"+{LowestStatBonus * 100}% to your lowest stat";
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
                plrChanges.moveSpeed.percentage += LowestStatBonus;
            else if (lowest.Item1 == "AS")
                plrChanges.attackSpeed.percentage += LowestStatBonus;
            else if (lowest.Item1 == "D")
                plrChanges.globalDamageMult.percentage += LowestStatBonus;
            else if (lowest.Item1 == "C")
                plrChanges.cooldownRed.percentage += LowestStatBonus;
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
        const int BaseProjectiles = 10;
        const int ProjectilesPerStack = 5;

        public override string ItemName => "Gasoline";
        public override string itemDescription => $"On kill, create {BaseProjectiles} (+{ProjectilesPerStack} per stack) gasoline projectiles";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                for (int i = 0; i < (ProjectilesPerStack * count) + BaseProjectiles; i++)
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
        const int HpPerStack = 10;

        public override string ItemName => "Small Kit";
        public override string itemDescription => $"+{HpPerStack} max hp per stack";
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
            NewMovement.Instance.GetHealth(HpPerStack, true);
        }
        public override void OnUpdate(int count)
        {
            hpChange.addition = HpPerStack * count;
        }
        public override void OnRemoval()
        {
            hpChange.addition = 0;
        }
    }
    public class SandWorm : BaseItem
    {
        const float DamagePerStack = 0.35f;

        public override string ItemName => "Sand Worm";
        public override string itemDescription => $"Sanded enemies take +{DamagePerStack * 100}% more damage";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c == 0) return 1;
                if (!eid.sandified) return 1f;
                return 1f + (DamagePerStack * c);
            });
        }
    }

    public class KnuckleDuster : BaseItem
    {
        const float DamagePerStack = 0.5f;

        public override string ItemName => "Knuckle Duster";
        public override string itemDescription => $"Arm damage +{DamagePerStack * 100}%";
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
            damageChange.damageChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class SpeedLoader : BaseItem
    {
        const float DamagePerStack = 0.35f;

        public override string ItemName => "Heavy Loader";
        public override string itemDescription => $"Revolver damage +{DamagePerStack * 100}%";
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
            damageChange.damageChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class RunningShoes : BaseItem
    {
        const float SpeedPerStack = 0.2f;

        public override string ItemName => "Running Shoes";
        public override string itemDescription => $"Move speed +{SpeedPerStack * 100}%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change moveChange;

        public override void OnStart()
        {
            moveChange = new Change(percentage: 0);
            new PlayerChange(moveSpeed: moveChange);
        }

        public override void OnUpdate(int count)
        {
            moveChange.percentage = SpeedPerStack * count;
        }

        public override void OnRemoval()
        {
            moveChange.percentage = 0;
        }
    }

    public class APRounds : BaseItem
    {
        const float DamagePerStack = 0.15f;

        public override string ItemName => "AP Rounds";
        public override string itemDescription => $"Railcannon damage +{DamagePerStack * 100}%";
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
            damageChange.damageChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class LooseNails : BaseItem
    {
        const float DamagePerStack = 0.25f;

        public override string ItemName => "Loose Nails";
        public override string itemDescription => $"Nailgun damage +{DamagePerStack * 100}%";
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
            damageChange.damageChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class PogoStick : BaseItem
    {
        const float JumpHeightPerStack = 0.05f;
        const float SlamDamagePerStack = 0.5f;

        public override string ItemName => "Pogo Stick";
        public override string itemDescription => $"Jump Height +{JumpHeightPerStack * 100}% and slam damage +{SlamDamagePerStack * 100}%";
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

                if (eid.hitter == "ground slam") return 1f + (SlamDamagePerStack * count);

                return 1f;
            });
        }

        public override void OnUpdate(int count)
        {
            jumpChange.percentage = JumpHeightPerStack * count;
        }

        public override void OnRemoval()
        {
            jumpChange.percentage = 0;
        }
    }

    public class IronSights : BaseItem
    {
        const float DamagePerStack = 0.12f;

        public override string ItemName => "Iron Sights";
        public override string itemDescription => $"+{DamagePerStack * 100}% damage per stack";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change dmgChange;

        public override void OnStart()
        {
            dmgChange = new Change(percentage: 0);
            new PlayerChange(globalDamageMult: dmgChange);
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            dmgChange.percentage = 0;
        }
    }

    public class GuttertankHand : BaseItem
    {
        const float DamagePerStack = 0.22f;

        public override string ItemName => "Gutter tank Hand";
        public override string itemDescription => $"Rocket Launcher damage +{DamagePerStack * 100}%";
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
            damageChange.damageChange.percentage = DamagePerStack * count;
        }

        public override void OnRemoval()
        {
            damageChange.damageChange.percentage = 0;
        }
    }

    public class Test : ActiveItem
    {
        const float DamageBonus = 0.50f;

        public override string ItemName => "Damage Book";
        public override int ChargeRequired => 3;
        public override string itemDescription => $"On activation, gain {DamageBonus * 100}% damage temporarily (resets when exiting combat).";
        Change change = new Change();
        PlayerChange plr;
        public override void OnStart()
        {
            plr = new PlayerChange(globalDamageMult: change);
        }
        public override bool CanAutoActivate()
        {
            return Room.isFighting;
        }

        public override void OnUse()
        {
            change.percentage = DamageBonus;
        }
        public override void OnUpdate(int count)
        {
            if (!Room.isFighting) change.percentage = 0;
        }
    }
}
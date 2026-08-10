using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Enemy;
using Ultrarogue.Characters;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ultrarogue.Items
{
    public class IgnitionTank : BaseItem
    {
        const float DamagePerStack = 1f;

        public override string ItemName => "Ignition Tank";
        public override string itemDescription => $"Fire deals {DamagePerStack * 100}% more damage";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int itemCount = Plugin.GetItemCount(this);
                if (eid.hitter == "fire")
                {
                    return 1f + (itemCount * DamagePerStack);
                }

                return 1f;
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

    public class BowlLasagna : BaseItem
    {
        const float PercentPerStack = 0.15f;

        public override string ItemName => "Bowl of Lasagna";
        public override string itemDescription => $"Increase all stats by +{PercentPerStack * 100}%";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        Change change = new Change();
        PlayerChange plr;
        public override void OnStart()
        {
            plr = new PlayerChange(change, null, change, change, change, null, globalDamageMult: change);
        }

        public override void OnUpdate(int count)
        {
            if (Plugin.SelectedChar.HasPassive(Passive.HealFromBlood))
                plr.maxHealth = new Change();
            else
                plr.maxHealth = change;
            change.percentage = PercentPerStack * count;
        }
        public override void OnRemoval()
        {
            change.percentage = 0;
        }
    }

    public class Panopticon : BaseItem
    {
        const int BaseHeal = 5;
        const int MaxHealsPerRoom = 7;

        public override Rarity Rarity => Rarity.Uncommon;
        public override string ItemName => "Panopticon";
        public override string itemDescription => $"On damage taken, heal {BaseHeal} (+{BaseHeal} per stack) hp up to {MaxHealsPerRoom} times per room.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing, ItemTag.Health };


        int HealAmount = MaxHealsPerRoom;

        public override void OnStart()
        {
            new DamageTakenEffect(ItemName, (dmg) =>
            {
                int c = Plugin.GetItemCount(this);

                if (c <= 0) return;
                if (HealAmount <= 0) return;
                NewMovement.Instance.GetHealth(BaseHeal * c, true, bloodsplatter: false);
                HealAmount--;
                Object.Instantiate(AssetsManager.healingEffect, NewMovement.Instance.transform.position, Quaternion.identity);
            });

        }

        bool wasCombat = false;

        public override void OnUpdate(int count)
        {
            if (!wasCombat && Room.isFighting)
            {
                HealAmount = MaxHealsPerRoom;
                wasCombat = true;
            }

            if (!Room.isFighting)
            {
                wasCombat = false;
            }
            base.OnUpdate(count);
        }
    }


    public class Executioner : BaseItem
    {
        const float HpThreshold = 0.20f;
        const float DamagePerStack = 1.0f;

        public override string ItemName => "Executioner";
        public override string itemDescription => $"Enemies below {HpThreshold * 100}% HP take {DamagePerStack * 100}% more damage (+{DamagePerStack * 100}% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return 1f;

                float hpPercent = eid.health / eid.GetComponent<Enemy>().originalHealth;
                if (hpPercent < HpThreshold)
                    return 1f + (DamagePerStack * count);

                return 1f;
            });
        }
    }

    public class Fusion : BaseItem
    {
        const float HpPerKill = 1f;
        const float MaxHpCap = 50f;

        public override string ItemName => "Fusion";
        public override string itemDescription => $"Each kill permanently increases your max hp by {HpPerKill} with a max of {MaxHpCap} (+{MaxHpCap} per stack)";
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
                if (killBonus >= MaxHpCap * count) return;
                killBonus += HpPerKill;
                if (Plugin.SelectedChar.GetType() != typeof(Filth))
                    NewMovement.Instance.GetHealth((int)HpPerKill, true);
            });
        }

        public override void OnUpdate(int count)
        {
            hpChange.addition = killBonus;
        }

        public override void OnRemoval()
        {
            killBonus = 0f;
            hpChange.addition = 0;
        }
    }

    public class Combatblood : BaseItem
    {
        const int HealPerStack = 6;

        public override string ItemName => "Combat blood";
        public override string itemDescription => $"On kill, restore {HealPerStack} HP (+{HealPerStack} per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing, ItemTag.Health };
        public override Rarity Rarity => Rarity.Uncommon;
        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return;

                int heal = HealPerStack * count;
                NewMovement.Instance.hp = Mathf.Min(NewMovement.Instance.hp + heal, Plugin.MaxHealth);
            });
        }
    }

    public class WillOWisp : BaseItem
    {
        const float Chance = 35f;
        const float BaseDamageMultiplier = 3.5f;
        const float DamagePerStackIncrement = 0.5f;
        const float BaseRadius = 6f;
        const float RadiusPerStack = 2f;

        public override string ItemName => "Will-o'-the-Maurice";
        public override string itemDescription => $"On kill, {Chance}% chance to detonate the corpse for {BaseDamageMultiplier * 100}% damage in a {BaseRadius}m radius (+{RadiusPerStack}m and +{DamagePerStackIncrement * 100}% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override string ItemIconName => "WillOMaurice";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                if (!Plugin.canExecute(Chance, "")) return;
                Plugin.Instance.StartCoroutine(explody(eid.transform.position));
            });
        }

        IEnumerator explody(Vector3 position)
        {
            yield return new WaitForSeconds(0.25f);
            int count = Plugin.GetItemCount(this);

            float radius = BaseRadius + (RadiusPerStack * count - 1);
            float damage = BaseDamageMultiplier + (DamagePerStackIncrement * count - 1);

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
        const float BaseChance = 25f;
        const float ChancePerStack = 15f;

        public override string ItemName => "Bouncy Hitscans";
        public override string itemDescription => $"All hitscans have a {BaseChance}% (+{ChancePerStack}% chance per stack) to bounce (chance gets smaller every bounce)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        public static void Prefix(RevolverBeam __instance)
        {
            if (!Plugin.isInRogueScene()) return;
            int count = Plugin.GetItemCount("Bouncy Hitscans");
            if (count <= 0) return;
            if (__instance.hasBeenRicocheter) return;
            if (__instance.beamType == BeamType.Enemy) return;
            if (__instance.beamType == BeamType.MaliciousFace) return;
            float baseChance = BaseChance + (ChancePerStack * (count - 1));

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
        const float HpThreshold = 0.25f;
        const float DamagePerStack = 0.75f;

        public override string ItemName => "Dead Man's Hand";
        public override string itemDescription => $"Below {HpThreshold * 100}% HP, deal {DamagePerStack * 100}% more damage (+{DamagePerStack * 100}% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return 1f;

                float hpPercent = (float)NewMovement.Instance.hp / Plugin.MaxHealth;
                if (hpPercent < HpThreshold)
                    return 1f + (DamagePerStack * count);

                return 1f;
            });
        }
    }

    public class NailBomb : BaseItem
    {
        const float DamageMultiplierPerStack = 1.5f;
        const float RadiusPerStack = 5f;

        public override string ItemName => "Nail Bomb";
        public override string itemDescription => $"Nailgun kills explode for {DamageMultiplierPerStack * 100}% damage in a {RadiusPerStack}m radius (+{RadiusPerStack}m per stack)";
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
                exp.maxSize = RadiusPerStack * count;
                exp.canHit = AffectedSubjects.EnemiesOnly;
                exp.damage = Mathf.RoundToInt(DamageMultiplierPerStack * count);
            }
        }
    }

    public class SpikyNails : BaseItem
    {
        const float DamagePerStack = 0.001f;

        public override string ItemName => "Spiky Nails";
        public override string itemDescription => $"Enemies with nail get +{DamagePerStack * 100}% (+{DamagePerStack * 100}% per stack) more damage per nail stuck in them.";
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
                float damageIncrease = (DamagePerStack * count) * nailsStuck;
                return damageIncrease + 1;
            });
        }
    }

    public class MissleLauncher : BaseItem
    {
        const float Chance = 10f;
        const float DamagePerStack = 3f;

        public override string ItemName => "Missle Launcher";
        public override string itemDescription => $"{Chance}% chance to launch a missile that deals {DamagePerStack * 100}% (+{DamagePerStack * 100}% per stack) base damage to an enemy";

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
                if (!Plugin.canExecute(Chance, eid.hitter)) return;

                float damage = DamagePerStack * count;
                GameObject missle = getMissleModel();
                Missle proj = missle.GetOrAddComponent<Missle>();
                proj.isRocket = true;
                proj.damage = damage;
                proj.enemyThatGotHit = eid;
                missle.transform.position = CameraController.Instance.GetDefaultPos() + Vector3.up * 3.5f;
            });
        }
    }

    [HarmonyPatch]
    public class Repeater : ActiveItem
    {
        const float DamageMultiplier = 2f;

        public override string ItemName => "Repeater";
        public override int ChargeRequired => 2;
        public override string itemDescription => $"Deal double the damage of your last attack. If the original target is dead, deal it to a random enemy instead.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override Rarity Rarity => Rarity.Uncommon;

        static EnemyIdentifier lastHit;
        static float lastHitDmg;
        static string lastHitter;

        bool DamagedEnemy = false;

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                lastHit = eid;
                lastHitDmg = dmg;
                lastHitter = eid.hitter;
                DamagedEnemy = true;
            }, true);
        }
        public override bool CanAutoActivate()
        {
            List<EnemyIdentifier> eids = EnemyTracker.Instance.GetCurrentEnemies();
            return DamagedEnemy && eids.Count > 0;
        }
        public override void OnUse()
        {
            if (lastHit == null || lastHit.dead)
            {
                List<EnemyIdentifier> eids = EnemyTracker.Instance.GetCurrentEnemies();
                lastHit = eids[Random.Range(0, eids.Count)];
            }

            lastHit.hitter = lastHitter;
            lastHit.DeliverDamage(lastHit.gameObject, Vector3.zero, lastHit.transform.position, lastHitDmg * DamageMultiplier, false);

            lastHit = null;
            DamagedEnemy = false;
        }

        public override void OnRemoval()
        {
            lastHit = null;
            lastHitDmg = 0;
            lastHitter = null;
        }
    }

    public class ChainRocket : BaseItem
    {
        const float BaseChance = 5f;
        const float ChancePerStack = 2f;
        const int RocketCount = 3;

        public override string ItemName => "Chain Rocket";
        public override Rarity Rarity => Rarity.Uncommon;

        public override void OnStart()
        {
            base.OnStart();
            new ProjectileCollideEffect(ItemName, (proj, type, other) =>
            {
                if (type != ProjectileType.Rocket) return;
                int c = Plugin.GetItemCount(this);
                if (c <= 0) return;

                EnemyIdentifier eid;
                if (!Plugin.TryGetEnemy(other, out eid)) return;

                if (Plugin.canExecute(BaseChance + (ChancePerStack * c - 1), eid.hitter))
                {
                    for (int i = 0; i < RocketCount; i++)
                    {
                        StartCoroutine(SpawnRocket(eid, 0.15f * i));
                        
                    }

                }
            });
        }

        IEnumerator SpawnRocket(EnemyIdentifier eid, float delay)
        {
            yield return new WaitForSeconds(delay);
            List<EnemyIdentifier> eids = EnemyTracker.Instance.GetCurrentEnemies();
            eids.RemoveAll((x) => x == eid);
            if (eids.Count <= 0) yield break;

            EnemyIdentifier target = eids[Random.Range(0, eids.Count)];
            Transform targetTransform = target.weakPoint ? target.weakPoint.transform : target.transform;
            Transform spawnPos = eid.weakPoint ? eid.weakPoint.transform : eid.transform;

            GameObject rocketObj = Object.Instantiate(AssetsManager.Rocket, spawnPos.position, Quaternion.identity);
            rocketObj.transform.forward = (targetTransform.position - rocketObj.transform.position).normalized;
            Collider rocketCol = rocketObj.GetComponent<Collider>();
            if (rocketCol != null)
            {
                foreach (Collider enemyCol in eid.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(rocketCol, enemyCol, true);
                }
            }
        }
    }
}
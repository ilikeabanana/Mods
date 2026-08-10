using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ultrarogue.Items
{

    public class LuckyLeaf : BaseItem
    {
        public override string ItemName => "Lucky Leaf";
        public override string itemDescription => "Luck based items are more likely to trigger";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Legendary;
        public override void OnGotten(int count, bool firstPickup)
        {
            Plugin.luck = count;
        }

        public override void OnRemoval()
        {
            Plugin.luck = 0;
        }
    }

    public class Dice : ActiveItem
    {
        public override string ItemName => "Dice";
        public override string itemDescription => "Reroll items";
        public override Rarity Rarity => Rarity.Legendary;
        public override int ChargeRequired => 5;
        public override void OnUse()
        {
            Room currentRoom = Room.getObjectInsideRoom(NewMovement.Instance.transform.position);
            ItemPickup[] pickups = currentRoom.GetComponentsInChildren<ItemPickup>();
            foreach (var item in pickups)
            {
                DroptableType drop = DroptableType.CommonOnly;

                switch (item.item.Rarity)
                {
                    case Rarity.Legendary:
                        drop = DroptableType.LegendaryOnly;
                        break;
                    case Rarity.Uncommon:
                        drop = DroptableType.UncommonOnly;
                        break;
                    case Rarity.Common:
                        drop = DroptableType.CommonOnly;
                        break;
                    case Rarity.Alchemy:
                        drop = DroptableType.Planetarium;
                        break;
                }

                BaseItem randomItem = Plugin.GiveRandomItem(RogueDifficultyManager.ItemRNG, drop);
                item.SwitchItem(randomItem, RemoveCondition: false, delay: 1);
            }

        }
    }

    [HarmonyPatch]
    public class ToolbarsFavorite : BaseItem
    {
        const float BounceMultiplier = 2f;
        const float BaseSpacing = 20f;
        const float SpacingDecayPerStack = 0.85f;
        const float MinSpacing = 2f;
        const int MaxZaps = 25;

        public override string ItemName => "Thunder Boomerang";
        public override string itemDescription => $"Double the hitscan bounce count. Every {BaseSpacing} (-{(1 - SpacingDecayPerStack) * 100}% per stack) units a hitscan travels, it zaps nearby enemies.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Legendary;

        internal static readonly HashSet<RevolverBeam> taggedBeams = new HashSet<RevolverBeam>();
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };
        public override float SpawnWeight => 0.8f; // Slightly lower spawn weight
        private static void SpawnExplosion(Vector3 point)
        {
            EnemyIdentifier.Zap(point, 0.5f);
            //GameObject go = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.explosion, point, Quaternion.identity);
            //foreach (Explosion exp in go.GetComponentsInChildren<Explosion>())
            //    exp.canHit = AffectedSubjects.EnemiesOnly;
        }

        [HarmonyPatch(typeof(RevolverBeam), "Shoot")]
        class ShootPatch
        {
            static void Postfix(RevolverBeam __instance)
            {
                if (!Plugin.isInRogueScene())
                    return;

                if (Plugin.GetItemCount("Thunder Boomerang") == 0)
                    return;
                if (__instance.beamType == BeamType.Enemy) return;
                if (__instance.beamType == BeamType.MaliciousFace) return;
                LineRenderer lr = __instance.GetComponent<LineRenderer>();

                Vector3 start = lr.GetPosition(0);
                Vector3 end = lr.GetPosition(1);

                float distance = Vector3.Distance(start, end);
                Vector3 direction = (end - start).normalized;

                int stacks = Plugin.GetItemCount("Thunder Boomerang");

                // Clamp spacing to something sane — don't let it collapse toward zero
                float zapSpacing = BaseSpacing * Mathf.Pow(SpacingDecayPerStack, stacks - 1);
                zapSpacing = Mathf.Max(zapSpacing, MinSpacing); // was 0.0001f

                // Hard cap on total zap points regardless of distance/stacks
                int zapCount = Mathf.Min(MaxZaps, Mathf.FloorToInt(distance / zapSpacing));

                for (int n = 1; n <= zapCount; n++)
                {
                    float i = n * zapSpacing;
                    if (i >= distance) break;
                    SpawnExplosion(start + direction * i);
                }
            }
        }
        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        [HarmonyPriority(Priority.Last)]
        public static void Prefix(RevolverBeam __instance)
        {
            if (!Plugin.isInRogueScene()) return;
            int count = Plugin.GetItemCount("Thunder Boomerang");
            if (count <= 0) return;
            if (__instance.hasBeenRicocheter) return;
            if (__instance.beamType == BeamType.Enemy) return;
            if (__instance.beamType == BeamType.MaliciousFace) return;
            __instance.ricochetAmount *= Mathf.RoundToInt(BounceMultiplier);
            if (__instance.hitAmount < 2) __instance.hitAmount = 2;
        }
    }

    public class BloodFlowingPlating : BaseItem
    {
        const float HealingPercentPerStack = 10f;

        public override string ItemName => "Blood Flowing Plating";
        public override string itemDescription => $"Have {HealingPercentPerStack}% of v1's healing (+{HealingPercentPerStack}% per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing, ItemTag.Health };
        public override Rarity Rarity => Rarity.Legendary;
    }

    public class StyleBalls : BaseItem
    {
        const float StyleThreshold = 100f;
        const float DamagePerThresholdPerStack = 0.5f;
        const float CheckInterval = 5f;
        const int MinMultiplierToLaunch = 4;

        public override string ItemName => "Hell's Opinion";
        public override string itemDescription => $"every {StyleThreshold} style gotten, gain {DamagePerThresholdPerStack * 100}% (+{DamagePerThresholdPerStack * 100}% per stack) damage for the style orbs. After {CheckInterval} seconds, if gathered over {MinMultiplierToLaunch * DamagePerThresholdPerStack * 100}% damage, launch a style orb.";

        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Legendary;

        int styleStart = 0;

        float timer = 0;

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

            timer += Time.deltaTime;

            if (timer >= CheckInterval)
            {
                int gainedStyle = StatsManager.Instance.stylePoints - styleStart;
                int mult = Mathf.CeilToInt(gainedStyle / StyleThreshold);
                if (mult >= MinMultiplierToLaunch)
                {
                    styleStart = StatsManager.Instance.stylePoints;
                    Plugin.Logger.LogInfo($"Launching orb with damage {mult * (DamagePerThresholdPerStack * count)}");
                    Launch(mult * (DamagePerThresholdPerStack * count));
                }
                timer = 0;
            }
        }
        bool attempted = false;
        GameObject missleModel = null;
        GameObject getMissleModel()
        {
            if (!attempted)
            {
                attempted = true;
                missleModel = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/AuraProjectile.prefab").WaitForCompletion();
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
        public void Launch(float damage)
        {
            GameObject missle = getMissleModel();
            Missle proj = missle.AddComponent<Missle>();
            proj.speed *= 3.5f;
            proj.damage = damage;
            missle.transform.position = CameraController.Instance.GetDefaultPos() + Vector3.up * 3.5f;
        }
    }
    public class PrimeHead : BaseItem
    {
        const float CooldownReductionPerStack = 0.60f;

        public override string ItemName => "Prime Head";
        public override string itemDescription => $"Cooldowns reduce by {CooldownReductionPerStack * 100}%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Legendary;
        Change change = new Change(percentage: 0);

        public override void OnStart()
        {
            new PlayerChange(cooldownReduction: change);
        }

        public override void OnUpdate(int count)
        {
            change.percentage = CooldownReductionPerStack * count;
        }

        public override void OnRemoval()
        {
            change.percentage = 0;
        }
    }

    public class VinnyPimpHat : BaseItem
    {
        const float FireInterval = 5f;
        const float DamagePerStack = 1.5f;

        public override string ItemName => "Vinny's Pimp Hat";
        public override string itemDescription => $"Every {FireInterval} seconds fire a purple saw that deals {DamagePerStack * 100}% (+{DamagePerStack * 100}% per stack) damage and stays until the room is cleared.";

        public override Rarity Rarity => Rarity.Legendary;
        public override List<Plugin.Weapon> WeaponProvisions => new List<Plugin.Weapon>() { Plugin.Weapon.Nailgun };
        float t = 0;
        bool wasPreviouslyFighting = false;

        GameObject sawPrefab = null;

        public override void OnUpdate(int count)
        {
            if (!Plugin.isInRogueScene()) return;
            if (count <= 0) return;

            if (wasPreviouslyFighting && !Room.isFighting)
            {
                Nail[] allNails = GameObject.FindObjectsOfType<Nail>();
                foreach (var nail in allNails)
                {
                    if (!nail.sawblade) continue;
                    if (nail.gameObject.name.Contains("SawVinny"))
                    {
                        Object.Destroy(nail.gameObject);
                    }
                }
            }

            if (Room.isFighting)
            {
                t += Time.deltaTime;

                if (t >= FireInterval)
                {
                    if (sawPrefab == null)
                        sawPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/SawVinny.prefab").WaitForCompletion();

                    FireSaw(DamagePerStack * count);
                    t = 0;
                }
            }
            wasPreviouslyFighting = Room.isFighting;
        }

        public override void OnRemoval()
        {
            // Reset the timer so no saw fires immediately if the item is re-acquired
            t = 0;
            wasPreviouslyFighting = false;

            // Destroy any saws that are still alive in the world
            Nail[] allNails = GameObject.FindObjectsOfType<Nail>();
            foreach (var nail in allNails)
            {
                if (!nail.sawblade) continue;
                if (nail.gameObject.name.Contains("SawVinny"))
                {
                    Object.Destroy(nail.gameObject);
                }
            }
        }

        void FireSaw(float damage)
        {
            float currentSpread = 2f;
            GameObject gameObject2 = Object.Instantiate<GameObject>(sawPrefab, CameraController.Instance.GetDefaultPos(), CameraController.Instance.transform.rotation);

            gameObject2.transform.Rotate(Random.Range(-currentSpread / 3f, currentSpread / 3f), Random.Range(-currentSpread / 3f, currentSpread / 3f), Random.Range(-currentSpread / 3f, currentSpread / 3f));
            Rigidbody rigidbody;
            if (gameObject2.TryGetComponent<Rigidbody>(out rigidbody))
            {
                rigidbody.velocity = gameObject2.transform.forward * 200f;
            }
            Nail nail;
            if (gameObject2.TryGetComponent<Nail>(out nail))
            {
                nail.damage = damage;
                nail.hitAmount = float.MaxValue - 1;
            }

            KeepInBoundsRoom kibr = gameObject2.AddComponent<KeepInBoundsRoom>();

            kibr.RoomInside = Room.getObjectInsideRoom(NewMovement.Instance.transform.position);
            kibr.ResetVelocity = false;
        }
    }

    public class AgonizedMask : BaseItem
    {
        const float BaseChance = 10f;
        const float ChancePerStack = 5f;

        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Agonized Mask";
        public override string itemDescription => $"Have a {BaseChance}% (+{ChancePerStack}% per stack) for an enemy to spawn as a puppet (does NOT include bosses)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
    }


    public class DualGun : BaseItem
    {
        const float BaseChance = 5f;
        const float ChancePerStack = 10f;

        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Dual Gun";
        public override string itemDescription => $"Have a {BaseChance}% (+{ChancePerStack}% per stack) to get a dual wield";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override bool RequiresAtleastOneWeapon => true;
    }
    [HarmonyPatch]
    public class EyeOfGod : BaseItem
    {
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Eye of God";

        public override string itemDescription =>
            $"{BaseChance}% chance on hit to call down a virtue beam dealing {BaseDamage * 100}% base damage. " +
            $"Every 100% damage dealt increases activation chance by {ChancePerHundred}% (+{ChancePerHundred}% per stack) " +
            $"and beam damage by {DamagePerHundred * 100}% (+{DamagePerHundred * 100}% per stack).";

        public override float SpawnWeight => 0.75f;
        public override List<ItemTag> itemTags =>
            new List<ItemTag>() { ItemTag.Utility };

        private const float BaseChance = 3f;
        private const float ChancePerHundred = 3f;
        private const float MaxChance = 75f;

        private const float BaseDamage = 1.5f;
        private const float DamagePerHundred = 0.5f;
        private const float MaxDamageMultiplier = 7.5f;

        // GLOBAL accumulated damage
        private static float accumulatedDamage = 0f;

        const int MaximumBeamAmount = 3;

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);

                if (count <= 0) return;
                if (eid.hitter == "fire") return;
                if (eid.hitter == "godseye") return;

                int bC = GameObject.FindObjectsByType<GodBeam>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).Length;
                if (bC >= MaximumBeamAmount) return;

                // Add damage dealt globally
                accumulatedDamage += dmg / Plugin.globalDamageMult.CalculateChanges(1f);

                // Every full 1.0 damage = one bonus step
                int thresholds = Mathf.FloorToInt(accumulatedDamage);

                float procChance =
                    BaseChance + (thresholds * ChancePerHundred * count);

                procChance = Mathf.Min(procChance, MaxChance);

                if (!Plugin.canExecute(procChance, eid.hitter))
                    return;

                float damageMultiplier =
                    BaseDamage + (thresholds * DamagePerHundred * count);

                damageMultiplier =
                    Mathf.Min(damageMultiplier, MaxDamageMultiplier);

                GameObject virtueBeam = Object.Instantiate(
                    AssetsManager.VirtueBeam,
                    eid.transform.position,
                    Quaternion.identity
                );
                if (virtueBeam.TryGetComponent<VirtueInsignia>(out var insig))
                {
                    insig.target = new EnemyTarget(eid);
                    insig.damage = Mathf.RoundToInt(damageMultiplier);
                    insig.windUpSpeedMultiplier = 2;
                }
                virtueBeam.name += "God";
                virtueBeam.AddComponent<GodBeam>();
                // RESET AFTER PROC
                accumulatedDamage = 0f;
            });
        }

        class GodBeam : MonoBehaviour
        {
            // nuthin
        }


        static Dictionary<VirtueInsignia, List<EnemyIdentifier>> alreadyHits = new Dictionary<VirtueInsignia, List<EnemyIdentifier>>();
        [HarmonyPatch(typeof(VirtueInsignia), nameof(VirtueInsignia.OnTriggerEnter))]
        public static bool Prefix(VirtueInsignia __instance, Collider other)
        {
            if (!Plugin.isInRogueScene()) return true;
            if (!__instance.gameObject.name.Contains("God")) return true;
            if (!alreadyHits.ContainsKey(__instance))
                alreadyHits.Add(__instance, new List<EnemyIdentifier>());
            if (__instance.target != null && (!__instance.target.isPlayer || other.gameObject.CompareTag("Player")))
            {

                EnemyIdentifier enemyIdentifier = other.GetComponent<EnemyIdentifier>();
                if (enemyIdentifier == null)
                {
                    EnemyIdentifierIdentifier component = other.GetComponent<EnemyIdentifierIdentifier>();
                    if (component != null)
                    {
                        enemyIdentifier = component.eid;
                    }
                }
                Rigidbody rigidbody;
                if (enemyIdentifier != null && other.TryGetComponent<Rigidbody>(out rigidbody) && !alreadyHits[__instance].Contains(enemyIdentifier))
                {
                    rigidbody.AddExplosionForce(1000f, __instance.transform.position, 10f);
                    enemyIdentifier.hitter = "godseye";
                    enemyIdentifier.SimpleDamage((float)__instance.damage);
                    alreadyHits[__instance].Add(enemyIdentifier);
                }

            }
            Flammable component2 = other.GetComponent<Flammable>();
            if (component2 && !component2.playerOnly)
            {
                component2.Burn(10f, false);
            }
            return false;
        }

    }

    public class JumperCable : BaseItem
    {
        const float GrowthRate = 0.05f;
        const float BaseChance = 0.10f;
        const float MaxChance = 0.20f;

        public override string ItemName => "Jumper Cable";
        public override string itemDescription => $"Enemies have a {BaseChance * 100}% chance to be shocked when a saw blade hits them. (+{GrowthRate * 100}% per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Nailgun };

        public override Rarity Rarity => Rarity.Legendary;
        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c <= 0 || eid.hitter != "sawblade") return;

                float chance = Plugin.LogarithmicChance(c - 1, GrowthRate, BaseChance, MaxChance) * 100;
                if (Plugin.canExecute(chance, "", false))
                {
                    eid.hitter = "zapper";
                    eid.hitterAttributes.Add(HitterAttribute.Electricity);
                    eid.DeliverDamage(eid.gameObject, Vector3.up * 1000f, eid.transform.position, 10f, true, 0f, null, false, false);
                    foreach (EnemyIdentifierIdentifier enemyIdentifierIdentifier in eid.GetComponentsInChildren<EnemyIdentifierIdentifier>())
                    {
                        Object.Instantiate<GameObject>(AssetsManager.zapThingy, enemyIdentifierIdentifier.transform.position, Quaternion.identity).transform.localScale *= 0.5f;
                    }
                }
            });
        }
    }

    [HarmonyPatch]
    public class ResidualCannon : BaseItem
    {
        const float DurationPerStack = 0.5f;
        const float DamageMultiplier = 10f;

        public override string ItemName => "Residual Cannon";
        public override string itemDescription => $"On hitscan fire, create a continuous beam that stays for {DurationPerStack}s (+{DurationPerStack}s per stack) and deals {DamageMultiplier * 100}% TOTAL damage";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Legendary;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };

        // No OnRemoval needed — the patch already gates on GetItemCount("Residual Cannon") > 0.

        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        public static void Postfix(RevolverBeam __instance)
        {
            if (!Plugin.isInRogueScene()) return;
            int count = Plugin.GetItemCount("Residual Cannon");
            if (count <= 0) return;
            if (__instance.beamType == BeamType.Enemy) return;
            if (__instance.beamType == BeamType.MaliciousFace) return;

            GameObject beam = Object.Instantiate(AssetsManager.mindflayerBeam, __instance.transform.position, __instance.transform.rotation);
            if (beam.TryGetComponent<ContinuousBeam>(out ContinuousBeam bem))
            {
                bem.damage = __instance.damage * DamageMultiplier;
                bem.canHitPlayer = false;
                bem.canHitEnemy = true;
            }

            if (beam.TryGetComponent<LineRenderer>(out LineRenderer lr))
            {
                lr.startColor = __instance.lr.startColor;
                lr.endColor = __instance.lr.endColor;
                lr.colorGradient = __instance.lr.colorGradient;
            }
            Object.Destroy(beam, DurationPerStack * count);
        }
    }

    public class Soulcatcher : BaseItem
    {
        const float DamagePerKill = 0.1f;

        public override string ItemName => "Soulcatcher";
        public override string itemDescription => $"Each kill increases damage by {DamagePerKill * 100}% for the room";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change dmgChange;
        float killBonus = 0f;

        public override void OnStart()
        {
            dmgChange = new Change(percentage: 0);
            new PlayerChange(globalDamageMult: dmgChange);

            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                killBonus += DamagePerKill;
            });
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = killBonus;
            if (!Room.isFighting)
                killBonus = 0;
        }

        public override void OnRemoval()
        {
            // Reset the accumulated kill bonus so it doesn't carry over if re-acquired
            killBonus = 0f;
            dmgChange.percentage = 0;
        }
    }

    [HarmonyPatch]
    public class CerberusHead : BaseItem
    {
        const float BaseSizeBonus = 0.5f;
        const float SizeBonusPerStack = 0.25f;
        const float BaseDamageMultiplier = 2f;
        const float DamageBonusPerStack = 0.5f;

        public override string ItemName => "Cerberus Head";
        public override string itemDescription => $"All Explosions caused by the player (rockets, projectile boosts, instakills) are {BaseSizeBonus * 100}% larger and do {(BaseDamageMultiplier - 1) * 100}% more damage ({SizeBonusPerStack * 100}% larger and {DamageBonusPerStack * 100}% damage per stack) (Your explosions no longer damage you)";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };


        [HarmonyPatch(typeof(Explosion), nameof(Explosion.Start))]
        public static void Prefix(Explosion __instance)
        {
            int c = Plugin.GetItemCount("Cerberus Head");

            if (c <= 0) return;

            if (__instance.enemy) return;

            __instance.maxSize *= 1f + BaseSizeBonus + SizeBonusPerStack * (c - 1);
            __instance.damage = Mathf.RoundToInt(__instance.damage * (BaseDamageMultiplier + DamageBonusPerStack * (c - 1)));

            __instance.hasHitPlayer = true;
        }
    }

    public class WarMachine : BaseItem
    {
        const float AttackSpeedPerStack = 0.45f;
        const float MoveSpeedPerStack = 0.20f;

        public override string ItemName => "War Machine";
        public override string itemDescription => $"Attack speed +{AttackSpeedPerStack * 100}%, move speed +{MoveSpeedPerStack * 100}%";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        Change atkChange;
        Change moveChange;

        public override void OnStart()
        {
            atkChange = new Change(percentage: 0);
            moveChange = new Change(percentage: 0);
            new PlayerChange(attackSpeed: atkChange, moveSpeed: moveChange);
        }

        public override void OnUpdate(int count)
        {
            atkChange.percentage = AttackSpeedPerStack * count;
            moveChange.percentage = MoveSpeedPerStack * count;
        }

        public override void OnRemoval()
        {
            atkChange.percentage = 0;
            moveChange.percentage = 0;
        }
    }

    public class HellsFire : BaseItem
    {
        const float DamagePerStack = 1f;

        public override string ItemName => "Hell's Fire";
        public override string itemDescription => $"Enemies on fire take +{DamagePerStack * 100}% more damage";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.RocketLauncher };
        // No OnRemoval needed — HitEffect and DamageModifier both gate on GetItemCount > 0.

        public override void OnStart()
        {

            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || eid.dead || eid.hitter != "fire") return 1f;
                Flammable[] flams = eid.flammables.ToArray();
                foreach (var f in flams)
                {
                    if (f.burning)
                        return 1f + (DamagePerStack * count);
                }
                return 1f;
            });
        }
    }

    public class MachineVirus : BaseItem
    {
        const float DamagePerHitPerStack = 0.005f;

        public override string ItemName => "Machine Virus";
        public override string itemDescription => $"Increase damage by {DamagePerHitPerStack * 100}% for every time that enemy was hit.";

        Dictionary<EnemyIdentifier, int> hits = new Dictionary<EnemyIdentifier, int>();
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c == 0) return 1f;

                int hit = 0;
                if (!hits.TryGetValue(eid, out hit))
                {
                    hits.Add(eid, hit = 1);
                    hit = 1;
                }
                hits[eid]++;
                return 1 + ((DamagePerHitPerStack * c) * hit);
            });
        }

        public override void OnRemoval()
        {
            // Clear tracked hit counts so stale data doesn't persist into future runs
            hits.Clear();
        }
    }
}
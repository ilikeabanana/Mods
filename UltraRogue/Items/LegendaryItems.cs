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
    [HarmonyPatch]
    public class ToolbarsFavorite : BaseItem
    {
        public override string ItemName => "Toolbar's favorite";
        public override string itemDescription => "Double the hitscan bounce count. All hitscan attacks explode now.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Legendary;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };

        internal static readonly HashSet<RevolverBeam> taggedBeams = new HashSet<RevolverBeam>();

        private static void SpawnExplosion(Vector3 point)
        {
            GameObject go = Object.Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.explosion, point, Quaternion.identity);
            foreach (Explosion exp in go.GetComponentsInChildren<Explosion>())
            {
                exp.canHit = AffectedSubjects.EnemiesOnly;
            }
        }

        [HarmonyPatch(typeof(RevolverBeam), "Start")]
        static class StartPatch
        {
            // Changed to Prefix so the bounce count is doubled BEFORE the gun fires
            static void Prefix(RevolverBeam __instance)
            {
                if (Plugin.GetItemCount("Toolbar's favorite") == 0) return;
                if (__instance.beamType == BeamType.Enemy || __instance.beamType == BeamType.MaliciousFace) return;

                taggedBeams.Add(__instance);

                if (__instance.previouslyHitTransform == null)
                {
                    __instance.ricochetAmount *= 2 + (Plugin.GetItemCount("Toolbar's favorite") - 1);
                }
            }
        }

        [HarmonyPatch(typeof(RevolverBeam), "HitSomething")]
        static class HitSomethingPatch
        {
            static void Postfix(RevolverBeam __instance, PhysicsCastResult hit)
            {
                if (!taggedBeams.Contains(__instance)) return;
                if (__instance.hitAmount != 1) return;

                SpawnExplosion(hit.point);
                taggedBeams.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(RevolverBeam), "PiercingShotCheck")]
        static class PiercingShotCheckPatch
        {
            // Struct to safely pass multiple state variables from Prefix to Postfix
            struct BeamState
            {
                public bool fadeOut;
                public Vector3 pos;
            }

            static void Prefix(RevolverBeam __instance, out BeamState __state)
            {
                __state = new BeamState
                {
                    fadeOut = __instance.fadeOut,
                    pos = __instance.shotHitPoint // Fallback if no hits
                };

                // Capture the precise point of impact before the game logic loses it
                if (__instance.hitList != null && __instance.enemiesPierced < __instance.hitList.Count)
                {
                    __state.pos = __instance.hitList[__instance.enemiesPierced].point;
                }
                else if (__instance.hitList != null && __instance.hitList.Count > 0)
                {
                    __state.pos = __instance.hitList[__instance.hitList.Count - 1].point;
                }
            }

            static void Postfix(RevolverBeam __instance, BeamState __state)
            {
                if (!taggedBeams.Contains(__instance)) return;

                // If the beam is terminating or bouncing on this step, spawn the explosion
                if (!__state.fadeOut && __instance.fadeOut)
                {
                    SpawnExplosion(__state.pos);
                    taggedBeams.Remove(__instance);
                }
            }
        }
    }

    public class PrimeHead : BaseItem
    {
        public override string ItemName => "Prime Head";
        public override string itemDescription => "Cooldowns reduce by 50%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Legendary;
        Change change = new Change(percentage: 0);

        public override void OnStart()
        {
            new PlayerChange(cooldownReduction: change);
        }

        public override void OnUpdate(int count)
        {
            change.percentage = 0.50f * count;
        }

        public override void OnRemoval()
        {
            change.percentage = 0;
        }
    }

    public class VinnyPimpHat : BaseItem
    {
        public override string ItemName => "Vinny's Pimp Hat";
        public override string itemDescription => "Every 3 seconds fire a purple saw that deals 150% (+150% per stack) damage and stays until the room is cleared.";

        public override Rarity Rarity => Rarity.Legendary;
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

                if (t >= 3)
                {
                    if (sawPrefab == null)
                        sawPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/SawVinny.prefab").WaitForCompletion();

                    FireSaw(1.5f * count);
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
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Agonized Mask";
        public override string itemDescription => "Have a 25% (+10% per stack) for an enemy to spawn as a puppet (does NOT include bosses)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
    }

    public class DualGun : BaseItem
    {
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Dual Gun";
        public override string itemDescription => "Have a 20% (+15% per stack) to get a dual wield";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
    }
    public class EyeOfGod : BaseItem
    {
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Eye of God";

        public override string itemDescription =>
            "3% chance on hit to call down a virtue beam dealing 300% base damage. " +
            "Every 100% damage dealt increases activation chance by 3% (+3% per stack) " +
            "and beam damage by 100% (+50% per stack).";

        public override List<ItemTag> itemTags =>
            new List<ItemTag>() { ItemTag.Utility };

        private const float BaseChance = 3f;
        private const float ChancePerHundred = 3f;
        private const float MaxChance = 75f;

        private const float BaseDamage = 3f;
        private const float DamagePerHundred = 1f;
        private const float MaxDamageMultiplier = 75f;

        // GLOBAL accumulated damage
        private static float accumulatedDamage = 0f;

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);

                if (count <= 0) return;
                if (eid.hitter == "fire") return;

                // Add damage dealt globally
                accumulatedDamage += dmg / Plugin.globalDamageMult.CalculateChanges(1f);

                // Every full 1.0 damage = one bonus step
                int thresholds = Mathf.FloorToInt(accumulatedDamage);

                float procChance =
                    BaseChance + (thresholds * ChancePerHundred * count);

                procChance = Mathf.Min(procChance, MaxChance);

                if (!Plugin.canExecute(procChance, ""))
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
                }

                // RESET AFTER PROC
                accumulatedDamage = 0f;
            });
        }
    }

    public class JumperCable : BaseItem
    {
        public override string ItemName => "Jumper Cable";
        public override string itemDescription => "Enemies have a 10% chance to be shocked when a saw blade hits them. (+5% per stack)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override Rarity Rarity => Rarity.Legendary;
        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int c = Plugin.GetItemCount(this);
                if (c <= 0 || eid.hitter != "sawblade") return;

                float chance = Plugin.LogarithmicChance(c - 1, 0.05f, 0.10f, 0.20f) * 100;
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
        public override string ItemName => "Residual Cannon";
        public override string itemDescription => "On hitscan fire, create a continuous beam that stays for 0.5s (+0.5s per stack) and deals 100% TOTAL damage";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Legendary;
        public override List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>() { Plugin.Weapon.Revolver };

        // No OnRemoval needed — the patch already gates on GetItemCount("Residual Cannon") > 0.

        [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.Start))]
        public static void Postfix(RevolverBeam __instance)
        {
            int count = Plugin.GetItemCount("Residual Cannon");
            if (count <= 0) return;
            if (__instance.beamType == BeamType.Enemy) return;
            if (__instance.beamType == BeamType.MaliciousFace) return;

            GameObject beam = Object.Instantiate(AssetsManager.mindflayerBeam, __instance.transform.position, __instance.transform.rotation);
            if (beam.TryGetComponent<ContinuousBeam>(out ContinuousBeam bem))
            {
                bem.damage = __instance.damage * 10f;
                bem.canHitPlayer = false;
                bem.canHitEnemy = true;
            }

            if (beam.TryGetComponent<LineRenderer>(out LineRenderer lr))
            {
                lr.startColor = __instance.lr.startColor;
                lr.endColor = __instance.lr.endColor;
                lr.colorGradient = __instance.lr.colorGradient;
            }
            Object.Destroy(beam, 0.5f * count);
        }
    }

    public class Soulcatcher : BaseItem
    {
        public override string ItemName => "Soulcatcher";
        public override string itemDescription => "Each kill permanently increases damage by 1% up to +150% (+150% per stack)";
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
                if (killBonus >= 1.5f * count) return;
                killBonus += 0.01f;
            });
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = killBonus;
        }

        public override void OnRemoval()
        {
            // Reset the accumulated kill bonus so it doesn't carry over if re-acquired
            killBonus = 0f;
            dmgChange.percentage = 0;
        }
    }

    public class CerberusHead : BaseItem
    {
        public override string ItemName => "Cerberus Head";
        public override string itemDescription => "All weapons deal +70% more damage";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change dmgChange;

        public override void OnStart()
        {
            dmgChange = new Change(percentage: 0);
            new PlayerChange(globalDamageMult: dmgChange);
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = 0.70f * count;
        }

        public override void OnRemoval()
        {
            dmgChange.percentage = 0;
        }
    }

    public class WarMachine : BaseItem
    {
        public override string ItemName => "War Machine";
        public override string itemDescription => "Attack speed +45%, move speed +20%";
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
            atkChange.percentage = 0.45f * count;
            moveChange.percentage = 0.20f * count;
        }

        public override void OnRemoval()
        {
            atkChange.percentage = 0;
            moveChange.percentage = 0;
        }
    }

    public class HellsFire : BaseItem
    {
        public override string ItemName => "Hell's Fire";
        public override string itemDescription => "Enemies on fire take +100% more damage";
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
                        return 1f + count;
                }
                return 1f;
            });
        }
    }

    public class MachineVirus : BaseItem
    {
        public override string ItemName => "Machine Virus";
        public override string itemDescription => "Increase damage by 0.5% for every time that enemy was hit.";

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
                return 1 + ((0.005f * c) * hit);
            });
        }

        public override void OnRemoval()
        {
            // Clear tracked hit counts so stale data doesn't persist into future runs
            hits.Clear();
        }
    }
}
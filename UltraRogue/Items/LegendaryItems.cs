using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Ultrarogue.Items
{

    public class LuckyLeaf : BaseItem
    {
        public override string ItemName => "Lucky Leaf";
        public override string itemDescription => "+1 luck";
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
        public override string itemDescription => "Every 5 seconds fire a purple saw that deals 150% (+150% per stack) damage and stays until the room is cleared.";

        public override Rarity Rarity => Rarity.Legendary;
        float t = 0;
        bool wasPreviouslyFighting = false;

        GameObject sawPrefab = null;

        public override void OnUpdate(int count)
        {
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

                if (t >= 5)
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
        }
    }

    public class AgonizedMask : BaseItem
    {
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Agonized Mask";
        public override string itemDescription => "Have a 25% (+10% per stack) for an enemy to spawn as a puppet (does NOT include bosses)";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
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
        public override string itemDescription => "Each kill permanently increases global damage by 1% with a maximum of +150% (+150% per stack)";
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
        public override string itemDescription => "All weapons deal +60% more damage";
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
            dmgChange.percentage = 0.60f * count;
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
        public override string itemDescription => "All hits ignite enemies, enemies on fire take +100% more damage";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        // No OnRemoval needed — HitEffect and DamageModifier both gate on GetItemCount > 0.

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || eid.dead) return;

                float burnDuration = 1.5f + (0.5f * (count - 1));

                if (eid.flammables != null && eid.flammables.Count > 0)
                {
                    eid.StartBurning(burnDuration);
                }
                else
                {
                    Flammable f = eid.GetComponentInChildren<Flammable>();
                    if (f != null) f.Burn(burnDuration, false);
                    else if(f == null)
                    {
                        eid.AddFlammable(10);
                        eid.StartBurning(burnDuration);
                    }
                }
            });
            
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
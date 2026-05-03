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
    }

    public class PrimeHead : BaseItem
    {
        public override string ItemName => "Prime Head";
        public override string itemDescription => "Cooldowns reduce by 20%";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Legendary;
        Change change = new Change(percentage: 0);

        public override void OnStart()
        {
            new PlayerChange(cooldownReduction: change);
        }

        public override void OnUpdate(int count)
        {
            change.percentage = 0.20f * count;
        }
    }

    public class VinnyPimpHat : BaseItem
    {
        public override string ItemName => "Vinny's Pimp Hat";
        public override string itemDescription => "Every 15 seconds fire a purple saw that deals 150% (+150% per stack) damage and stays until the room is cleared.";

        public override Rarity Rarity => Rarity.Legendary;
        float t = 0;
        bool wasPreviouslyFighting = false;

        GameObject sawPrefab = null;
        public override void OnUpdate(int count)
        {
            if (count <= 0) return;

            if(wasPreviouslyFighting && !Room.isFighting)
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

                if(t >= 5)
                {
                    // FIRE THE SAW!!!
                    if (sawPrefab == null)
                        sawPrefab = Addressables.LoadAssetAsync<GameObject>("Assets/Modding/RogueMode/SawVinny.prefab").WaitForCompletion();

                    FireSaw(1.5f * count);
                    t = 0;
                }
            }
            wasPreviouslyFighting = Room.isFighting;
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
            }
        }
    }
    public class AgonizedMask : BaseItem
    {
        public override Rarity Rarity => Rarity.Legendary;
        public override string ItemName => "Agonized Mask";
        public override string itemDescription => "On Kill spawn a puppet as an ally";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                if (!Plugin.canExecute(7f * Plugin.GetItemCount(this), "")) return;
                EnemyType type = eid.enemyType;
                GameObject? prefab = DefaultReferenceManager.Instance?.GetEnemyPrefab(type);
                if (prefab != null)
                {
                    GameObject instantiated = Object.Instantiate(prefab, eid.transform.position, eid.transform.rotation);
                    instantiated.AddComponent<TeamComponent>().teamId = Team.Player;
                    instantiated.GetComponent<EnemyIdentifier>().puppet = true;
                }
            });
        }
    }

    public class Soulcatcher : BaseItem
    {
        public override string ItemName => "Soulcatcher";
        public override string itemDescription => "Each kill permanently increases global damage by 0.5% (+0.5% per stack)";
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
                killBonus += 0.005f * count;
            });
        }

        public override void OnUpdate(int count)
        {
            dmgChange.percentage = killBonus;
        }
    }

    public class CerberusHead : BaseItem
    {
        public override string ItemName => "Cerberus Head";
        public override string itemDescription => "All weapons deal +100% more damage";
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
            dmgChange.percentage = 1f * count;
        }
    }
    public class WarMachine : BaseItem
    {
        public override string ItemName => "War Machine";
        public override string itemDescription => "Attack speed +30%, move speed +20%";
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
            atkChange.percentage = 0.30f * count;
            moveChange.percentage = 0.20f * count;
        }
    }

    public class Executioner : BaseItem
    {
        public override string ItemName => "Executioner";
        public override string itemDescription => "Enemies below 20% HP take 100% more damage (+100% per stack)";
        public override Rarity Rarity => Rarity.Legendary;
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

    public class HellsFire : BaseItem
    {
        public override string ItemName => "Hell's Fire";
        public override string itemDescription => "All hits ignite enemies";
        public override Rarity Rarity => Rarity.Legendary;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || eid.dead || eid.hitter == "fire") return;

                float burnDuration = 1.5f + (0.5f * (count - 1));

                if (eid.flammables != null && eid.flammables.Count > 0)
                {
                    eid.StartBurning(burnDuration);
                }
                else
                {
                    Flammable f = eid.GetComponentInChildren<Flammable>();
                    if (f != null) f.Burn(burnDuration, false);
                }
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
                if(!hits.TryGetValue(eid, out hit))
                {
                    hits.Add(eid, hit = 1);
                    hit = 1;
                }

                return 1 + ((0.5f*c) * hit);

            });
        }
    }
}
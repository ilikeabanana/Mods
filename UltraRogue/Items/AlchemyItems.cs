using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrarogue.Items
{
    public class Reaper : BaseItem
    {
        public override string ItemName => "Reaper's Scythe";
        public override string itemDescription => "Double your damage, <color=red>BUT LOSE 50% OF YOUR MAXIMUM HP</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Alchemy;
        Change dmg = new Change();
        Change HP = new Change();
        public override void OnStart()
        {
            new PlayerChange(globalDamageMult: dmg, maxHealth: HP);
        }
        public override void OnUpdate(int count)
        {
            float amountToThing = Mathf.Pow(0.5f, count);
            dmg.postMultiplier = count + 1;
            HP.postMultiplier = amountToThing;
        }
        public override void OnRemoval()
        {
            dmg.postMultiplier = 1;
            HP.postMultiplier = 1;
        }
    }
    public class FragileParts : BaseItem
    {
        public override string ItemName => "Fragile Parts";
        public override string itemDescription => "Double all your stats, <color=red>TAKING DAMAGE REDUCES VALUES BY 5%</color>";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change amazingChange = new Change();
        public override void OnGotten(int count, bool firstPickup)
        {
            amazingChange.percentage = count;
        }
        public override void OnStart()
        {
            new DamageTakenEffect(ItemName, (d) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                if (c <= 0 || d <= 0) return;

                amazingChange.percentage = Mathf.Max(-0.5f, amazingChange.percentage - (0.05f * c));
            });
            new PlayerChange(amazingChange, attackSpeed: amazingChange, cooldownReduction: amazingChange, globalDamageMult: amazingChange);
        }
    }
    public class WildCard : BaseItem
    {
        public override string ItemName => "Wild Card";
        public override string itemDescription => "Each room, <color=yellow>one random stat doubles</color>. <color=red>Another random stat is halved</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change buffedStat = new Change();
        Change nerfedStat = new Change();
        readonly Change[] allChanges;
        public WildCard()
        {
            allChanges = new Change[] { buffedStat, nerfedStat };
        }
        public override void OnStart()
        {
            new PlayerChange(allChanges[0], attackSpeed: allChanges[1]);
            
        }
        public override void RoomEnter()
        {
            Reroll();
        }
        void Reroll()
        {
            int count = Plugin.GetItemCount(ItemName);
            buffedStat.postMultiplier = 1f + (1f * count);
            nerfedStat.postMultiplier = Mathf.Pow(0.5f, count);
        }
        public override void OnRemoval()
        {
            buffedStat.postMultiplier = 1;
            nerfedStat.postMultiplier = 1;
        }
    }
    public class Overclock : BaseItem
    {
        public override string ItemName => "Overclock";
        public override string itemDescription => "Gain +50% attack speed and damage, but <color=red>-50% cooldown reduction</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change speedAndDmg = new Change();
        Change cooldownPenalty = new Change();
        public override void OnStart()
        {
            new PlayerChange(attackSpeed: speedAndDmg, globalDamageMult: speedAndDmg, cooldownReduction: cooldownPenalty);
        }
        public override void OnUpdate(int count)
        {
            speedAndDmg.percentage = 0.5f * count;
            cooldownPenalty.percentage = -1 * (0.50f * count);
        }
        public override void OnRemoval()
        {
            speedAndDmg.percentage = 0;
            cooldownPenalty.percentage = 0;
        }
    }
    public class DecayingEmpowerment : BaseItem
    {
        public override string ItemName => "Decaying Empowerment";
        public override string itemDescription => "Start with +100% to all stats, but they <color=red>decay over time</color>. Kills reset the decay.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change allStats = new Change();
        float decayTimer = 0f;
        const float decayRate = 0.02f;
        const float decayInterval = 1f;
        float nextDecayTime = 0f;
        public override void OnStart()
        {
            new PlayerChange(allStats, attackSpeed: allStats, cooldownReduction: allStats, globalDamageMult: allStats);
            new DeathEffect(ItemName, (enemy) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                allStats.percentage = c * 1.0f;
            });
        }
        public override void OnUpdate(int count)
        {
            if (Time.time >= nextDecayTime)
            {
                allStats.percentage = Mathf.Max(-0.7f, allStats.percentage - (decayRate * count));
                nextDecayTime = Time.time + decayInterval;
            }
        }
        public override void OnRemoval()
        {
            allStats.percentage = 0;
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            allStats.percentage = count * 1.0f;
        }
    }
    public class Gluttony : BaseItem
    {
        public override string ItemName => "Gluttony";
        public override string itemDescription => "Increase damage by 10% for every item, but reduce movement speed for by 5% every item.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        public Change everyOtherChange = new Change();
        public Change movementChange = new Change();
        public override void OnStart()
        {
            new PlayerChange(movementChange, globalDamageMult: everyOtherChange);
        }
        public override void OnUpdate(int count)
        {
            int itemCount = Plugin.items.Sum((x) => x.Value);
            everyOtherChange.percentage = itemCount * (0.10f * count);
            movementChange.percentage = -1 * (itemCount * (0.05f * count));
        }
    }
}
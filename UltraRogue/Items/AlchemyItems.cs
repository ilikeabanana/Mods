using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrarogue.Items
{
    public class Reaper : BaseItem
    {
        public override string ItemName => "Reaper's Scythe";
        public override string itemDescription => "Double your damage, <color=red>BUT LOSE 50% OF YOUR HP EVERY FLOOR</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Healing }; // Healing purely to prevent V1 from getting it
        public override Rarity Rarity => Rarity.Alchemy;
        Change dmg = new Change();
        public override void OnStart()
        {
            new PlayerChange(globalDamageMult: dmg);
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            dmg.multiplier = count * 2;
        }
        public override void OnNewFloor(int count)
        {
            if (count >= 1)
            {
                float amountToThing = Mathf.Pow(0.5f, count);
                float hp = NewMovement.Instance.hp * amountToThing; // health to have

                int toReduce = Mathf.FloorToInt(NewMovement.Instance.hp - hp);
                NewMovement.Instance.GetHurt(toReduce, false, ignoreInvincibility: true);
            }
        }
        public override void OnRemoval()
        {
            dmg.multiplier = 1;
        }
    }
    public class FragileParts : BaseItem
    {
        public override string ItemName => "Fragile Parts";
        public override string itemDescription => "Double all your stats, <color=red>TAKING DAMAGE REDUCES VALUES BY 10%</color>";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change amazingChange = new Change();
        Change damageReductionChange = new Change(); // separate, goes up on hit
        public override void OnGotten(int count, bool firstPickup)
        {
            amazingChange.percentage = count;
            damageReductionChange.percentage = -0.5f;
        }
        public override void OnStart()
        {
            new DamageTakenEffect(ItemName, (d) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                if (c <= 0 || d <= 0) return;

                amazingChange.percentage = Mathf.Max(-0.9f, amazingChange.percentage - (0.1f * c));
                damageReductionChange.percentage += Mathf.Min(0.1f * c, 1); // increases instead
            });
            new PlayerChange(amazingChange, attackSpeed: amazingChange, cooldownReduction: amazingChange, damageReduction: damageReductionChange, globalDamageMult: amazingChange);
        }
    }
    public class Gluttony : BaseItem
    {
        public override string ItemName => "Gluttony";
        public override string itemDescription => "Increase all stats by 10% for every item, but reduce movement speed for by 5% every item.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        public Change everyOtherChange = new Change();
        public Change movementChange = new Change();
        public override void OnStart()
        {
            new PlayerChange(movementChange, attackSpeed: everyOtherChange, cooldownReduction: everyOtherChange, damageReduction: everyOtherChange, globalDamageMult: everyOtherChange);
        }
        public override void OnUpdate(int count)
        {
            int itemCount = Plugin.items.Sum((x) => x.Value);
            everyOtherChange.percentage = itemCount * (0.10f * count);
            movementChange.percentage = -1 * (itemCount * (0.05f * count));
        }
    }
    public class CurseOfRa : BaseItem
    {
        public override string ItemName => "Curse of Ra";
        public override string itemDescription => "<color=red>ALL ENEMIES ARE SANDED</color>";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        public override bool CanOnlyHaveOne => true;
        
    }
}
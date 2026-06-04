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
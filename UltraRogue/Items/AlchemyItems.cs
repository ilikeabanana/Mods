using System;
using System.Collections.Generic;
using System.Linq;

namespace Ultrarogue.Items
{
    public class YingYang : BaseItem
    {
        public override string ItemName => "Ying Yang";
        public override string itemDescription => "Balances your stats.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;

        PlayerChange plr = new PlayerChange();

        public override void OnGotten(int count, bool firstPickup)
        {
            var others = Plugin.playerChanges
                .Where(p => p != plr)
                .ToList();

            if (others.Count == 0) return;

            float avgAddition = others.Select(p =>
                p.moveSpeed.addition +
                p.jumpHeight.addition +
                p.maxHealth.addition +
                p.attackSpeed.addition +
                p.cooldownRed.addition +
                p.globalDamageMult.addition
            ).Average() / 6f;

            float avgPercentage = others.Select(p =>
                p.moveSpeed.percentage +
                p.jumpHeight.percentage +
                p.maxHealth.percentage +
                p.attackSpeed.percentage +
                p.cooldownRed.percentage +
                p.globalDamageMult.percentage
            ).Average() / 6f;

            float avgMultiplier = others.Select(p =>
                p.moveSpeed.multiplier *
                p.jumpHeight.multiplier *
                p.maxHealth.multiplier *
                p.attackSpeed.multiplier *
                p.cooldownRed.multiplier *
                p.globalDamageMult.multiplier
            ).Average();
            avgMultiplier = (float)Math.Pow(avgMultiplier, 1.0 / 6.0);

            void Balance(Change target, Change avg)
            {
                target.addition = avg.addition - target.addition;
                target.percentage = avg.percentage - target.percentage;
                target.multiplier = avg.multiplier;
            }

            var avgChange = new Change(avgAddition, avgPercentage, avgMultiplier);

            Balance(plr.moveSpeed, avgChange);
            Balance(plr.jumpHeight, avgChange);
            Balance(plr.maxHealth, avgChange);
            Balance(plr.attackSpeed, avgChange);
            Balance(plr.cooldownRed, avgChange);
            Balance(plr.globalDamageMult, avgChange);
        }

        public override void OnRemoval()
        {
            plr.moveSpeed = new Change();
            plr.jumpHeight = new Change();
            plr.maxHealth = new Change();
            plr.attackSpeed = new Change();
            plr.cooldownRed = new Change();
            plr.globalDamageMult = new Change();
        }
    }
}
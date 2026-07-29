using System;
using System.Collections;
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

        public override Material materialOverride => AssetsManager.getAlchemy();
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

        public override void OnGotten(int count, bool firstPickup)
        {
            base.OnGotten(count, firstPickup);
        }
        IEnumerator ensureCorrectHP()
        {
            yield return new WaitForEndOfFrame();
            NewMovement.Instance.hp = Plugin.MaxHealth;
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
        public override string itemDescription => "Double all your stats, <color=red>TAKING DAMAGE REDUCES VALUES BY 2%</color>";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change amazingChange = new Change();
        public override Material materialOverride => AssetsManager.getAlchemy();
        public static FragileParts I { get; set; }
        public override void OnGotten(int count, bool firstPickup)
        {
            amazingChange.percentage = count;
        }

        public static void Reset()
        {
            I.amazingChange.percentage = Plugin.GetItemCount(I.ItemName);
        }
        public override void OnUpdate(int count)
        {
            float cap = count;
            if (amazingChange.percentage < cap)
                amazingChange.percentage = Mathf.Min(cap, amazingChange.percentage + (0.005f * count * Time.deltaTime));
        }

        public override void OnStart()
        {
            I = this;
            new DamageTakenEffect(ItemName, (d) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                if (c <= 0 || d <= 0) return;

                amazingChange.percentage = Mathf.Max(-0.5f, amazingChange.percentage - (0.02f * c));
            });
            new PlayerChange(amazingChange, attackSpeed: amazingChange, cooldownReduction: amazingChange, globalDamageMult: amazingChange);
        }
    }
    public class WildCard : ActiveItem
    {
        public override string ItemName => "Wild Card";
        public override string itemDescription => "On Activation, <color=yellow>one random stat doubles</color>. <color=red>Another random stat is halved</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        public override int ChargeRequired => 3;

        Change buffedStat = new Change();
        Change nerfedStat = new Change();
        Change neutral = new Change();
        public override Material materialOverride => AssetsManager.getAlchemy();
        // All 7 stat slots, order matches the enum below
        Change[] statSlots;

        enum Stat { MoveSpeed, AttackSpeed, CooldownRed, DamageReduction, GlobalDamageMult }
        const int StatCount = 5;

        public override void OnStart()
        {
            statSlots = new Change[StatCount];
            for (int i = 0; i < StatCount; i++)
                statSlots[i] = new Change(); // ← each gets its own object

            new PlayerChange(
                moveSpeed: statSlots[(int)Stat.MoveSpeed],
                attackSpeed: statSlots[(int)Stat.AttackSpeed],
                cooldownReduction: statSlots[(int)Stat.CooldownRed],
                damageReduction: statSlots[(int)Stat.DamageReduction],
                globalDamageMult: statSlots[(int)Stat.GlobalDamageMult]
            );
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            if(firstPickup)
                Reroll();
        }
        public override void OnUse()
        {
            Reroll();
        }

        void Reroll()
        {
            int count = Plugin.GetItemCount(ItemName);

            for (int i = 0; i < StatCount; i++)
                statSlots[i].postMultiplier = 1f;

            int buffIndex = UnityEngine.Random.Range(0, StatCount);
            int nerfIndex;
            do { nerfIndex = UnityEngine.Random.Range(0, StatCount); }
            while (nerfIndex == buffIndex);

            // Buff is now 2x per stack, nerf is only 0.6x (was 0.5x) — always net positive
            statSlots[buffIndex].postMultiplier = 1f + (1f * count);       // same
            statSlots[nerfIndex].postMultiplier = Mathf.Pow(0.5f, count); // was 0.5f
        }

        public override void OnRemoval()
        {
            for (int i = 0; i < StatCount; i++)
                statSlots[i].postMultiplier = 1f;
        }
    }
    public class Overclock : BaseItem
    {
        public override Material materialOverride => AssetsManager.getAlchemy();
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
        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Decaying Empowerment";
        public override string itemDescription => "Start with +100% to all stats, but they <color=red>decay over time</color>. Kills <color=green>restore some power</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change allStats = new Change();
        const float decayRate = 0.02f;     // was 0.05f — slower decay
        const float decayInterval = 1.5f;  // was 0.85f — longer between ticks
        float nextDecayTime = 0f;
        public override void OnStart()
        { 
            new PlayerChange(allStats, attackSpeed: allStats, cooldownReduction: allStats, globalDamageMult: allStats);
            new DeathEffect(ItemName, (enemy) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                allStats.percentage = Mathf.Min(c * 1.0f, allStats.percentage + (0.25f * c));
            });
        }
        public override void OnNewFloor(int count)
        {
            allStats.percentage = count * 1.0f;
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
        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Gluttony";
        public override string itemDescription => "Increase damage by 10% for every item, but reduce movement speed for by 2% every item.";
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

            // Cap the movement penalty at -40% regardless of item count
            float rawPenalty = itemCount * (0.02f * count); // was 0.05f
            movementChange.percentage = -1 * rawPenalty;
        }
    }
    public class Null : BaseItem
    {
        public static Null I;
        public override string NameDisplayOverride => "<voffset=2px><size=120%><color=#00ffff>N</color></size></voffset><voffset=4px><color=#ff00ff>U</color></voffset><voffset=-2px><size=80%><color=#ffff00>L</color></size></voffset><voffset=6px><color=#00ff00>L</color></voffset>";
        public override string ItemName => "Null";
        public override string itemDescription => "50% chance for every item to be replaced with a pure stat upgrade.";
        public override Material materialOverride => AssetsManager.getAlchemy();
        public override Rarity Rarity => Rarity.Alchemy;
        //fortitudo - Damage 10% up
        //velocitas - Speed 10% up
        //rapidiatis - Attack speed 10 % up
        //refrigescant - cooldown 10% up

        public override void OnStart()
        {
            base.OnStart();
            I = this;
        }
    }

    public class Fortitudo : BaseItem
    {
        public override string ItemName => "Fortitudo";
        public override string itemDescription => "Increase damage by 20%";
        public override Material materialOverride => AssetsManager.getAlchemy();
        Change c = new Change();
        public override Rarity Rarity => Rarity.NullItem;

        public override void OnStart()
        {
            base.OnStart();
            new PlayerChange(globalDamageMult: c);
        }
        public override void OnUpdate(int count)
        {
            base.OnUpdate(count);
            c.percentage = 0.30f * count;
        }
    }

    public class Velocitas : BaseItem
    {
        public override string ItemName => "Velocitas";
        public override string itemDescription => "Increase speed by 30%";
        public override Material materialOverride => AssetsManager.getAlchemy();
        Change c = new Change();
        public override Rarity Rarity => Rarity.NullItem;
        public override void OnStart()
        {
            base.OnStart();
            new PlayerChange(moveSpeed: c);
        }
        public override void OnUpdate(int count)
        {
            base.OnUpdate(count);
            c.percentage = 0.30f * count;
        }
    }

    public class Rapidiatis : BaseItem
    {
        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Rapidiatis";
        public override string itemDescription => "Increase attackspeed by 20%";

        Change c = new Change();
        public override Rarity Rarity => Rarity.NullItem;
        public override void OnStart()
        {
            base.OnStart();
            new PlayerChange(attackSpeed: c);
        }
        public override void OnUpdate(int count)
        {
            base.OnUpdate(count);
            c.percentage = 0.30f * count;
        }
    }

    public class Refrigescant : BaseItem
    {
        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Refrigescant";
        public override string itemDescription => "Increase cooldown reduction by 30%";

        Change c = new Change();
        public override Rarity Rarity => Rarity.NullItem;
        public override void OnStart()
        {
            base.OnStart();
            new PlayerChange(cooldownReduction: c);
        }
        public override void OnUpdate(int count)
        {
            base.OnUpdate(count);
            c.percentage = 0.30f * count;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultrarogue.Items
{
    public class Reaper : BaseItem
    {
        const float DamageMultiplierPerStack = 1f;
        const float HpMultiplierPerStack = 0.5f;
        const float HpSyncDelay = 0.12f;

        public override string ItemName => "Reaper's Scythe";
        public override string itemDescription => $"Multiply your damage by {1 + DamageMultiplierPerStack} per stack, <color=red>BUT LOSE {(1 - HpMultiplierPerStack) * 100}% OF YOUR MAXIMUM HP per stack</color>.";
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
            float amountToThing = Mathf.Pow(HpMultiplierPerStack, count);
            dmg.postMultiplier = count + DamageMultiplierPerStack;
            HP.postMultiplier = amountToThing;
        }

        public override void OnGotten(int count, bool firstPickup)
        {
            base.OnGotten(count, firstPickup);
        }
        IEnumerator ensureCorrectHP()
        {
            yield return new WaitForSeconds(HpSyncDelay);
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
        const float BonusPerStack = 1f;
        const float DecayPerHitPerStack = 0.02f;
        const float MinPercentage = -0.5f;

        public override string ItemName => "Fragile Parts";
        public override string itemDescription => $"Increase all your stats by {BonusPerStack * 100}% per stack, <color=red>TAKING DAMAGE REDUCES VALUES BY {DecayPerHitPerStack * 100}%</color>";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change amazingChange = new Change();
        public override Material materialOverride => AssetsManager.getAlchemy();
        public static FragileParts I { get; set; }
        public override void OnGotten(int count, bool firstPickup)
        {
            amazingChange.percentage = count * BonusPerStack;
        }

        public static void Reset()
        {
            I.amazingChange.percentage = Plugin.GetItemCount(I.ItemName) * BonusPerStack;
        }
        public override void OnNewFloor(int count)
        {
            base.OnNewFloor(count);
            Reset();
        }

        public override void OnStart()
        {
            I = this;
            new DamageTakenEffect(ItemName, (d) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                if (c <= 0 || d <= 0) return;

                amazingChange.percentage = Mathf.Max(MinPercentage, amazingChange.percentage - (DecayPerHitPerStack * c));
            });
            new PlayerChange(amazingChange, attackSpeed: amazingChange, cooldownReduction: amazingChange, globalDamageMult: amazingChange);
        }
    }
    public class WildCard : ActiveItem
    {
        const float BuffMultiplierPerStack = 1f;
        const float NerfMultiplierBase = 0.5f;

        public override string ItemName => "Wild Card";
        public override string itemDescription => "On Activation, <color=yellow>one random stat doubles</color>. <color=red>Another random stat is halved</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        public override int ChargeRequired => 3;

        Change buffedStat = new Change();
        Change nerfedStat = new Change();
        Change neutral = new Change();
        public override Material materialOverride => AssetsManager.getAlchemy();
        Change[] statSlots;

        enum Stat { MoveSpeed, AttackSpeed, CooldownRed, DamageReduction, GlobalDamageMult }
        const int StatCount = 5;

        public override void OnStart()
        {
            statSlots = new Change[StatCount];
            for (int i = 0; i < StatCount; i++)
                statSlots[i] = new Change();

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
            if (firstPickup)
                Reroll();
        }
        public override void OnUse()
        {
            Reroll();
        }

        public override bool CanAutoActivate()
        {
            return true;
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

            statSlots[buffIndex].postMultiplier = 1f + (BuffMultiplierPerStack * count);
            statSlots[nerfIndex].postMultiplier = Mathf.Pow(NerfMultiplierBase, count);
        }

        public override void OnRemoval()
        {
            for (int i = 0; i < StatCount; i++)
                statSlots[i].postMultiplier = 1f;
        }
    }
    public class Overclock : BaseItem
    {
        const float AttackAndDamagePerStack = 0.5f;
        const float CooldownPenaltyPerStack = 0.50f;

        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Overclock";
        public override string itemDescription => $"Gain +{AttackAndDamagePerStack * 100}% attack speed and damage, but <color=red>-{CooldownPenaltyPerStack * 100}% cooldown reduction</color>.";
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
            speedAndDmg.percentage = AttackAndDamagePerStack * count;
            cooldownPenalty.percentage = -1 * (CooldownPenaltyPerStack * count);
        }
        public override void OnRemoval()
        {
            speedAndDmg.percentage = 0;
            cooldownPenalty.percentage = 0;
        }
    }
    public class DecayingEmpowerment : BaseItem
    {
        const float InitialBonusPerStack = 1.0f;
        const float RestorePerKillPerStack = 0.25f;
        const float DecayRate = 0.02f;
        const float DecayInterval = 1.5f;
        const float MinPercentage = -0.7f;

        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Decaying Empowerment";
        public override string itemDescription => $"Start with +{InitialBonusPerStack * 100}% to all stats, but they <color=red>decay over time</color>. Kills <color=green>restore some power</color>.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage, ItemTag.Utility };
        public override Rarity Rarity => Rarity.Alchemy;
        Change allStats = new Change();
        float nextDecayTime = 0f;
        public override void OnStart()
        {
            new PlayerChange(allStats, attackSpeed: allStats, cooldownReduction: allStats, globalDamageMult: allStats);
            new DeathEffect(ItemName, (enemy) =>
            {
                int c = Plugin.GetItemCount(ItemName);
                allStats.percentage = Mathf.Min(c * InitialBonusPerStack, allStats.percentage + (RestorePerKillPerStack * c));
            });
        }
        public override void OnNewFloor(int count)
        {
            allStats.percentage = count * InitialBonusPerStack;
        }
        public override void OnUpdate(int count)
        {
            if (Time.time >= nextDecayTime)
            {
                allStats.percentage = Mathf.Max(MinPercentage, allStats.percentage - (DecayRate * count));
                nextDecayTime = Time.time + DecayInterval;
            }
        }
        public override void OnRemoval()
        {
            allStats.percentage = 0;
        }
        public override void OnGotten(int count, bool firstPickup)
        {
            allStats.percentage = count * InitialBonusPerStack;
        }
    }
    public class Gluttony : BaseItem
    {
        const float DamagePerItemPerStack = 0.10f;
        const float SpeedPenaltyPerItemPerStack = 0.02f;

        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Gluttony";
        public override string itemDescription => $"Increase damage by {DamagePerItemPerStack * 100}% for every item, but reduce movement speed by {SpeedPenaltyPerItemPerStack * 100}% for every item.";
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
            everyOtherChange.percentage = itemCount * (DamagePerItemPerStack * count);

            float rawPenalty = itemCount * (SpeedPenaltyPerItemPerStack * count);
            movementChange.percentage = -1 * rawPenalty;
        }
    }
    public class Null : BaseItem
    {
        const float ReplaceChance = 50f;

        public static Null I;
        public override string NameDisplayOverride => "<voffset=2px><size=120%><color=#00ffff>N</color></size></voffset><voffset=4px><color=#ff00ff>U</color></voffset><voffset=-2px><size=80%><color=#ffff00>L</color></size></voffset><voffset=6px><color=#00ff00>L</color></voffset>";
        public override string ItemName => "Null";
        public override string itemDescription => $"{ReplaceChance}% chance for every item to be replaced with a pure stat upgrade.";
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
        const float BonusPerStack = 0.30f;

        public override string ItemName => "Fortitudo";
        public override string itemDescription => $"Increase damage by {BonusPerStack * 100}%";
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
            c.percentage = BonusPerStack * count;
        }
    }

    public class Velocitas : BaseItem
    {
        const float BonusPerStack = 0.30f;

        public override string ItemName => "Velocitas";
        public override string itemDescription => $"Increase speed by {BonusPerStack * 100}%";
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
            c.percentage = BonusPerStack * count;
        }
    }

    public class Rapidiatis : BaseItem
    {
        const float BonusPerStack = 0.30f;

        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Rapidiatis";
        public override string itemDescription => $"Increase attackspeed by {BonusPerStack * 100}%";

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
            c.percentage = BonusPerStack * count;
        }
    }

    public class Refrigescant : BaseItem
    {
        const float BonusPerStack = 0.30f;

        public override Material materialOverride => AssetsManager.getAlchemy();
        public override string ItemName => "Refrigescant";
        public override string itemDescription => $"Increase cooldown reduction by {BonusPerStack * 100}%";

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
            c.percentage = BonusPerStack * count;
        }
    }
}
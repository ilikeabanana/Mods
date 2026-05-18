using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR;

namespace Ultrarogue.Characters
{
    public class RandomCharacter : BaseCharacter
    {
        // Cached so each LoadLevel re-roll happens once per play, not on every property access
        private List<AWeapon> _weapons;
        private List<string> _items;
        private List<Passive> _passives;

        public override string Name => "Scavanger";
        public override string Description => "Start with 2 random weapons, 1 random item, and 1 random passive. <color=red>ALL YOUR STATS ARE RANDOMIZED</color>";

        public override List<AWeapon> StartingWeapons => _weapons ??= RollWeapons();
        public override List<string> StartingItems => _items ??= RollItems();
        public override List<Passive> Passives => _passives ??= RollPassives();

        // Called by Plugin.LoadLevel before it reads the above properties,
        // so the cache is cleared and re-rolled fresh each run.
        public override void OnRunStart()
        {
            Rng = new System.Random(Plugin.GameSeed.GetHashCode() - 90);
            _weapons = null;
            _passives = null;
            _items = null;
            hasReset = false;

            pC.globalDamageMult = CreateRandomChange();
            pC.moveSpeed = CreateRandomChange();
            pC.jumpHeight = CreateRandomChange();
            pC.maxHealth = CreateRandomChange();
            pC.attackSpeed = CreateRandomChange();
            pC.cooldownRed = CreateRandomChange();
        }
        PlayerChange pC = new PlayerChange();
        bool hasReset = false;
        public override void Update(bool selected)
        {
            if (!selected && !hasReset)
            {
                hasReset = true;
                pC.globalDamageMult = new Change();
                pC.moveSpeed = new Change();
                pC.jumpHeight = new Change();
                pC.maxHealth = new Change();
                pC.attackSpeed = new Change();
                pC.cooldownRed = new Change();
            }
        }

        // ── helpers ────────────────────────────────────────────────────────────

        private static System.Random Rng = new System.Random();

        private static List<AWeapon> RollWeapons()
        {
            var allWeapons = Enum.GetValues(typeof(Plugin.Weapon)).Cast<Plugin.Weapon>().ToList();
            var allVariants = Enum.GetValues(typeof(Plugin.Variant)).Cast<Plugin.Variant>().ToList();
            var result = new List<AWeapon>();

            for (int i = 0; i < 2; i++)
            {
                var weapon = allWeapons[Rng.Next(allWeapons.Count)];
                var variant = allVariants[Rng.Next(allVariants.Count)];
                bool alt = AWeapon.CanBeAlternate(weapon) && Rng.NextDouble() <= 0.5;
                result.Add(new AWeapon(weapon, variant, alt));
            }
            return result;
        }

        static Change CreateRandomChange()
        {
            double min = 0.01;
            double max = 2;
            double scaledValue = min + (Rng.NextDouble() * (max - min));
            return new Change(multiplier: (float)scaledValue);
        }

        private static List<string> RollItems()
        {
            // possibleItems is filled by GatherItems() well before LoadLevel is called
            if (Plugin.possibleItems == null || Plugin.possibleItems.Count == 0)
                return new List<string>();

            var result = new List<string>();
            for (int i = 0; i < 1; i++)
            {
                var item = Plugin.possibleItems[Rng.Next(Plugin.possibleItems.Count)];
                result.Add(item.ItemName);
            }
            return result;
        }

        private static List<Passive> RollPassives()
        {
            var all = Enum.GetValues(typeof(Passive)).Cast<Passive>().ToList();

            Passive first = all[Rng.Next(all.Count)];

            return new List<Passive> { first };
        }
    }
}
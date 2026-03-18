using UnityEngine;
using Ultrarogue;

namespace Ultrarogue.Items
{
    // ───────────────────────────────────────────
    // COMMON
    // ───────────────────────────────────────────

    /// +15% move speed per stack
    public class RunningShoes : BaseItem
    {
        public override string ItemName => "Running Shoes";
        public override Rarity Rarity => Rarity.Common;

        private PlayerChange change;

        public override void OnStart()
        {
            change = new PlayerChange(moveSpeed: new Change(percentage: 0f));
        }

        public override void OnGotten(int count, bool firstTime)
        {
            change.moveSpeed.percentage = 0.15f * count;
        }
    }

    /// +10% jump height per stack
    public class SpringBoots : BaseItem
    {
        public override string ItemName => "Spring Boots";
        public override Rarity Rarity => Rarity.Common;

        private PlayerChange change;

        public override void OnStart()
        {
            change = new PlayerChange(jumpHeight: new Change(percentage: 0f));
        }

        public override void OnGotten(int count, bool firstTime)
        {
            change.jumpHeight.percentage = 0.10f * count;
        }
    }

    /// Heals 2hp on kill, +2hp per stack
    public class VampireTeeth : BaseItem
    {
        public override string ItemName => "Vampire Teeth";
        public override Rarity Rarity => Rarity.Common;

        public override void OnStart()
        {
            new DeathEffect(ItemName, (enemy) =>
            {
                int count = Plugin.GetItemCount(ItemName);
                int healAmount = 2 * count;
                // TODO: heal the player — need to know the heal method on NewMovement
                 NewMovement.Instance?.GetHealth(healAmount, false);
            });
        }

        public override void OnGotten(int count, bool firstTime) { }
    }

    /// Kill gives +40% speed for 3s, +3s duration per stack
    public class AdrenalineRush : BaseItem
    {
        public override string ItemName => "Adrenaline Rush";
        public override Rarity Rarity => Rarity.Common;

        private PlayerChange change;
        private float timer = 0f;

        public override void OnStart()
        {
            change = new PlayerChange(moveSpeed: new Change(percentage: 0f));

            new DeathEffect(ItemName, (enemy) =>
            {
                int count = Plugin.GetItemCount(ItemName);
                timer = 3f * count;
            });
        }

        public override void OnGotten(int count, bool firstTime) { }

        public override void OnUpdate(int count)
        {
            if (timer > 0)
            {
                timer -= Time.deltaTime;
                change.moveSpeed.percentage = 0.40f;
            }
            else
            {
                change.moveSpeed.percentage = 0f;
            }
        }
    }

    // ───────────────────────────────────────────
    // UNCOMMON
    // ───────────────────────────────────────────

    /// +25% speed multiplier and +15% jump multiplier per stack
    public class HyperspeedCore : BaseItem
    {
        public override string ItemName => "Hyperspeed Core";
        public override Rarity Rarity => Rarity.Uncommon;

        private PlayerChange change;

        public override void OnStart()
        {
            change = new PlayerChange(
                moveSpeed: new Change(multiplier: 1f),
                jumpHeight: new Change(multiplier: 1f)
            );
        }

        public override void OnGotten(int count, bool firstTime)
        {
            change.moveSpeed.multiplier = 1f + (0.25f * count);
            change.jumpHeight.multiplier = 1f + (0.15f * count);
        }
    }

    /// On kill, spawns an explosion — stacks spawn one extra explosion per stack
    public class DeathCharge : BaseItem
    {
        public override string ItemName => "Death Charge";
        public override Rarity Rarity => Rarity.Uncommon;

        public override void OnStart()
        {
            new DeathEffect(ItemName, (enemy) =>
            {
                if (!Plugin.canExecute(35f, enemy.hitter,)) return;
                int count = Plugin.GetItemCount(ItemName);
                for (int i = 0; i < count; i++)
                {
                    // TODO: spawn explosion prefab at enemy position — need the correct prefab path/reference
                     Vector3 offset = Random.insideUnitSphere * (i * 1.5f); // spread extra explosions out a bit
                     GameObject explosion = Object.Instantiate(DefaultReferenceManager.Instance.explosion, enemy.transform.position + offset, Quaternion.identity);
                }
            });
        }

        public override void OnGotten(int count, bool firstTime) { }
    }

    /// Below 35% hp: +100% speed per stack
    public class LastLegs : BaseItem
    {
        public override string ItemName => "Last Legs";
        public override Rarity Rarity => Rarity.Uncommon;

        private PlayerChange change;

        public override void OnStart()
        {
            change = new PlayerChange(moveSpeed: new Change(multiplier: 1f));
        }

        public override void OnGotten(int count, bool firstTime) { }

        public override void OnUpdate(int count)
        {
            if (NewMovement.Instance == null) return;

            // TODO: need to know the correct health/maxHealth property names on NewMovement
             float healthPercent = NewMovement.Instance.hp / (float)100f;
             change.moveSpeed.multiplier = healthPercent < 0.35f ? 1f + (1f * count) : 1f;
        }
    }

    // ───────────────────────────────────────────
    // LEGENDARY
    // ───────────────────────────────────────────

    /// Spawns one Filth familiar per stack
    public class BoundFilth : BaseItem
    {
        public override string ItemName => "Bound Filth";
        public override Rarity Rarity => Rarity.Legendary;

        public override void OnStart() { }

        public override void OnGotten(int count, bool firstTime)
        {
            // Spawn exactly one new familiar per stack gained (firstTime = first, !firstTime = additional stacks)
            // TODO: need to know the correct Filth prefab path and how to assign it to Team.Player
            GameObject filthPrefab = DefaultReferenceManager.Instance.filth; // Resources.Load or AssetBundle lookup for filth prefab
             GameObject familiar = Object.Instantiate(filthPrefab, NewMovement.Instance.transform.position + Vector3.forward * 2f, Quaternion.identity);
             var team = familiar.AddComponent<TeamComponent>();
             team.teamId = Team.Player;
        }
    }

    /// On kill: 20% base chance +10% per stack to recruit a copy of the enemy
    public class SoulHarvester : BaseItem
    {
        public override string ItemName => "Soul Harvester";
        public override Rarity Rarity => Rarity.Legendary;

        public override void OnStart()
        {
            new DeathEffect(ItemName, (enemy) =>
            {
                int count = Plugin.GetItemCount(ItemName);
                float chance = 20f + (10f * (count - 1)); // 20%, 30%, 40%...
                if (!Plugin.canExecute(chance, "")) return;

                // TODO: need to clone the killed enemy's GameObject and assign it Team.Player
                GameObject baseObj = DefaultReferenceManager.Instance.GetEnemyPrefab(enemy.enemyType);

                 GameObject clone = Object.Instantiate(baseObj, enemy.transform.position, enemy.transform.rotation);
                 clone.GetComponent<TeamComponent>().teamId = Team.Player;
            });
        }

        public override void OnGotten(int count, bool firstTime) { }
    }

    /// Survive a killing blow with 1hp — invincible for 3s (+2s per stack) — once per room
    public class Soulbound : BaseItem
    {
        public override string ItemName => "Soulbound";
        public override Rarity Rarity => Rarity.Legendary;

        public override void OnStart()
        {
            // TODO: patch NewMovement damage/death to intercept a killing blow
            // On trigger: grant (3 + 2 * (count - 1)) seconds of invincibility
            // Track "used this room" with a bool, reset it in SceneManager_sceneLoaded equivalent
        }

        public override void OnGotten(int count, bool firstTime) { }
    }
}
using System.Collections;
using System.Collections.Generic;
using ULTRAKILL.Enemy;
using UnityEngine;

namespace Ultrarogue.Items
{
    // ─────────────────────────────────────────────
    //  ORIGINAL ITEMS
    // ─────────────────────────────────────────────

    public class IgnitionTank : BaseItem
    {
        public override string ItemName => "Ignition Tank";
        public override string itemDescription => "Fire deals 100% more damage";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int itemCount = Plugin.GetItemCount(this);
                if (eid.hitter == "fire")
                {
                    return itemCount + 1;
                }

                return 1;
            });
        }
    }

    public class WillOWisp : BaseItem
    {
        public override string ItemName => "Will-o'-the-Wisp";
        public override string itemDescription => "On kill, detonate the corpse for 350% damage in a 6m radius (+6m and +350% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;

                Plugin.Instance.StartCoroutine(explody(eid.transform.position));
            });
        }

        IEnumerator explody(Vector3 position)
        {
            yield return new WaitForSeconds(0.25f);
            int count = Plugin.GetItemCount(this);

            float radius = 6f * count;
            float damage = 3.5f * count;

            GameObject explosion = Object.Instantiate(DefaultReferenceManager.Instance.explosion, position, Quaternion.identity);
            foreach (var exp in explosion.GetComponentsInChildren<Explosion>())
            {
                exp.maxSize = radius;
                exp.canHit = AffectedSubjects.EnemiesOnly;
                exp.damage = Mathf.RoundToInt(damage);
            }
        }
    }

    public class DeadMansHand : BaseItem
    {
        public override string ItemName => "Dead Man's Hand";
        public override string itemDescription => "Below 25% HP, deal 75% more damage (+75% per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return 1f;

                float hpPercent = (float)NewMovement.Instance.hp / Plugin.MaxHealth;
                if (hpPercent < 0.25f)
                    return 1f + (0.75f * count);

                return 1f;
            });
        }
    }
    public class GlassCannon : BaseItem
    {
        public override string ItemName => "Glass Cannon";
        public override string itemDescription => "Max HP -20%. Global damage +30%";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        Change hpChange;
        Change dmgChange;

        public override void OnStart()
        {
            hpChange = new Change(percentage: 0);
            dmgChange = new Change(percentage: 0);
            new PlayerChange(maxHealth: hpChange, globalDamageMult: dmgChange);
        }

        public override void OnUpdate(int count)
        {
            hpChange.percentage = -0.20f * count;
            dmgChange.percentage = 0.30f * count;
        }
    }

    public class NailBomb : BaseItem
    {
        public override string ItemName => "Nail Bomb";
        public override string itemDescription => "Nailgun kills explode for 150% damage in a 5m radius (+5m per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };

        public override void OnStart()
        {
            new DeathEffect(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;

                // Only proc on nailgun kills
                List<string> nailHitters = Plugin.WeaponToHitter(Plugin.Weapon.Nailgun);
                if (!nailHitters.Contains(eid.hitter)) return;

                Plugin.Instance.StartCoroutine(NailExplosion(eid.transform.position, count));
            });
        }

        IEnumerator NailExplosion(Vector3 position, int count)
        {
            yield return new WaitForEndOfFrame();

            GameObject explosion = Object.Instantiate(
                DefaultReferenceManager.Instance.explosion,
                position,
                Quaternion.identity
            );
            foreach (var exp in explosion.GetComponentsInChildren<Explosion>())
            {
                exp.maxSize = 5f * count;
                exp.canHit = AffectedSubjects.EnemiesOnly;
                exp.damage = Mathf.RoundToInt(1.5f * count);
            }
        }
    }

    public class LeechBullets : BaseItem
    {
        public override string ItemName => "Leeching Bullets";
        public override string itemDescription => "Revolver hits restore 1 HP (+1 per stack)";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Healing };

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0 || NewMovement.Instance == null) return;
                if (eid.hitter != "revolver") return;

                int heal = 1 * count;
                NewMovement.Instance.hp = Mathf.Min(NewMovement.Instance.hp + heal, Plugin.MaxHealth);
            });
        }
    }

    public class Overcharge : BaseItem
    {
        public override string ItemName => "Overcharge";
        public override string itemDescription => "Railcannon damage +25%";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange railChange;

        public override void OnStart()
        {
            railChange = new DamageChange(Plugin.Weapon.Railcannon, new Change(percentage: 0));
            new PlayerChange(
                damageChanges: new List<DamageChange>() { railChange }
            );
        }

        public override void OnUpdate(int count)
        {
            railChange.damageChange.percentage = 0.25f * count;
        }
    }

    public class FeatherFists : BaseItem
    {
        public override string ItemName => "Feather Fists";
        public override string itemDescription => "Arm damage +25%, move speed +10%";
        public override Rarity Rarity => Rarity.Uncommon;
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        DamageChange armChange;
        Change moveChange;

        public override void OnStart()
        {
            armChange = new DamageChange(Plugin.Weapon.Arm, new Change(percentage: 0));
            moveChange = new Change(percentage: 0);
            new PlayerChange(
                damageChanges: new List<DamageChange>() { armChange },
                moveSpeed: moveChange
            );
        }

        public override void OnUpdate(int count)
        {
            armChange.damageChange.percentage = 0.25f * count;
            moveChange.percentage = 0.10f * count;
        }
    }

    public class SpikyNails : BaseItem
    {
        public override string ItemName => "Spiky Nails";
        public override string itemDescription => "Enemies with nail get +1% (+1% per stack) more damage per nail stuck in them.";
        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Uncommon;

        public override void OnStart()
        {
            new DamageModifier(ItemName, (eid) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return 1f;

                int nailsStuck = eid.nailsAmount;
                float damageIncrease = (0.01f * count) * nailsStuck;
                return damageIncrease + 1;
            });
        }
    }

    public class MissleLauncher : BaseItem
    {
        public override string ItemName => "Missle Launcher";
        public override string itemDescription => "10% chance to launch a missle dealing 300% (+300% per stack) damage";

        public override List<ItemTag> itemTags => new List<ItemTag>() { ItemTag.Damage };
        public override Rarity Rarity => Rarity.Uncommon;

        public override void OnStart()
        {
            new HitEffect(ItemName, (eid, dmg) =>
            {
                int count = Plugin.GetItemCount(this);
                if (count <= 0) return;
                if(!Plugin.canExecute(10, eid.hitter)) return; 


                float damage = (3 * count);
                GameObject missle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                missle.GetComponent<Collider>().isTrigger = true;
                missle.AddComponent<Rigidbody>().useGravity = false;
                Missle proj = missle.AddComponent<Missle>();
                proj.damage = damage;
                proj.enemyThatGotHit = eid;
                missle.transform.position = CameraController.Instance.GetDefaultPos() + Vector3.up * 3.5f; 
            });
        }
    }
}
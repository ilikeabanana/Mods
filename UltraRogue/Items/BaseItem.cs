using System;
using System.Collections.Generic;
using System.Text;

namespace Ultrarogue.Items
{
    public abstract class BaseItem
    {
        public virtual string ItemName => "";
        public virtual string itemDescription => string.Empty;
        public virtual string ItemIconName => ItemName.Replace(" ", "_");
        public virtual Rarity Rarity => Rarity.Common;
        public virtual List<ItemTag> itemTags => new List<ItemTag>();
        public virtual void OnGotten(int count, bool firstPickup)
        {

        }
        public virtual void OnStart()
        {

        }

        public virtual void OnUpdate(int count)
        {

        }

        public override string ToString()
        {
            return $"Item name: {ItemName}, description: {itemDescription}";
        }

    }
    public enum ItemTag
    {
        Utility,
        Damage,
        Healing
    }
}

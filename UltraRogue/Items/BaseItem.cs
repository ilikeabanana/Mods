using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Ultrarogue.Items
{
    public abstract class ActiveItem : BaseItem
    {
        public virtual bool ConsumeOnUse => false;
        public virtual int ChargeRequired => 0;

        public virtual void OnUse()
        {

        }
    }

    public abstract class BaseItem
    {
        public virtual bool CanOnlyHaveOne => false;

        public virtual Material materialOverride => null;
        public virtual void OnMaterialApply(Material mat)
        {

        }

        public virtual string NameDisplayOverride => "";
        public virtual string ItemName => "";
        public virtual string itemDescription => string.Empty;
        public virtual string itemLore => "This is a placeholder. write something sad here :(";
        public virtual string ItemIconName => ItemName.Replace(" ", "_");
        public virtual Rarity Rarity => Rarity.Common;
        public virtual List<ItemTag> itemTags => new List<ItemTag>();
        public virtual List<Plugin.Weapon> WeaponRequirements => new List<Plugin.Weapon>();
        public virtual List<Plugin.Weapon> WeaponProvisions => new List<Plugin.Weapon>();
        public virtual float SpawnWeight => 1;
        public virtual void OnGotten(int count, bool firstPickup)
        {

        }
        public virtual void OnStart()
        {

        }
        public virtual void RoomEnter()
        {

        }
        public virtual void OnNewFloor(int count)
        {

        }

        public virtual void OnUpdate(int count)
        {

        }
        public virtual void OnRemoval()
        {

        }
        public void StartCoroutine(IEnumerator routine)
        {
            Plugin.Instance.StartCoroutine(routine);
        }

        public override string ToString()
        {
            string name = string.IsNullOrEmpty(NameDisplayOverride) ? ItemName : NameDisplayOverride;
            return $"Item name: {name}, description: {itemDescription}";
        }

        public Sprite ItemIcon
        {
            get
            {
                Texture2D tex = ItemTexture;
                if (tex == null) return null;

                return Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }

        public Texture2D ItemTexture
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"Ultrarogue.ItemIcons.{ItemIconName}.png";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Debug.LogWarning($"[ItemIcon] Resource not found: {resourceName}");
                        return null;
                    }

                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.LoadImage(buffer);
                    texture.filterMode = FilterMode.Point;
                    texture.name = ItemIconName;

                    return texture;
                }
            }
        }

    }
    public enum ItemTag
    {
        Utility,
        Damage,
        Healing,
        MaxHealth,
        Health
    }
}

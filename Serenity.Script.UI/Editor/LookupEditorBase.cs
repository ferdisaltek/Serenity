﻿using System.Html;
using jQueryApi;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Serenity
{
    [Element("<input type=\"hidden\"/>")]
    public abstract class LookupEditorBase<TOptions, TItem> : Select2Editor<TOptions, TItem>
        where TOptions: class, new()
        where TItem: class, new()
    {
        protected LookupEditorBase(jQueryObject hidden, TOptions opt)
            : base(hidden, opt)
        {
            var self = this;

            if (!IsAsyncWidget())
            {
                #pragma warning disable 618
                UpdateItems();
                Q.ScriptData.BindToChange("Lookup." + GetLookupKey(), this.uniqueName, () => self.UpdateItems());
                #pragma warning restore 618
            }
        }

        protected override Promise InitializeAsync()
        {
            return UpdateItemsAsync().Then(() => 
            {
                Q.ScriptData.BindToChange("Lookup." + GetLookupKey(), this.uniqueName, delegate()
                {
                    UpdateItemsAsync();
                });
            });
        }

        public override void Destroy()
        {
            Q.ScriptData.UnbindFromChange(this.uniqueName);
            element.Select2("destroy");

            base.Destroy();
        }

        protected virtual string GetLookupKey()
        {
            var key = this.GetType().FullName;
            var idx = key.IndexOf(".");
            if (idx >= 0)
                key = key.Substring(idx + 1);

            if (key.EndsWith("Editor"))
                key = key.Substring(0, key.Length - 6);

            return key;
        }

        protected virtual Lookup<TItem> GetLookup()
        {
            #pragma warning disable 618
            return Q.GetLookup<TItem>(GetLookupKey());
            #pragma warning restore 618
        }

        protected virtual Promise<Lookup<TItem>> GetLookupAsync()
        {
            return Promise.Void.ThenAwait(() => {
                var key = GetLookupKey();
                return Q.GetLookupAsync<TItem>(key);
            });
        }

        protected virtual IEnumerable<TItem> GetItems(Lookup<TItem> lookup)
        {
            return lookup.Items;
        }

        protected virtual string GetItemText(TItem item, Lookup<TItem> lookup)
        {
            object textValue = (lookup.TextFormatter != null ? lookup.TextFormatter(item) : ((dynamic)item)[lookup.TextField]);
            return textValue == null ? "" : textValue.ToString();
        }

        protected virtual bool GetItemDisabled(TItem item, Lookup<TItem> lookup)
        {
            return false;
        }

        protected virtual void UpdateItems()
        {
            #pragma warning disable 618
            var lookup = GetLookup();
            #pragma warning restore 618

            ClearItems();

            var items = GetItems(lookup);
            foreach (dynamic item in items)
            {
                var text = GetItemText(item, lookup);
                var disabled = GetItemDisabled(item, lookup);

                object idValue = item[lookup.IdField];
                string id = idValue == null ? "" : idValue.ToString();

                this.items.Add(new Select2Item
                {
                    Id = id,
                    Text = text,
                    Source = item,
                    Disabled = disabled
                });
            }
        }

        protected virtual Promise UpdateItemsAsync()
        {
            return GetLookupAsync().Then(lookup =>
            {
                ClearItems();

                var items = GetItems(lookup);
                foreach (dynamic item in items)
                {
                    var text = GetItemText(item, lookup);
                    var disabled = GetItemDisabled(item, lookup);

                    object idValue = item[lookup.IdField];
                    string id = idValue == null ? "" : idValue.ToString();

                    this.items.Add(new Select2Item
                    {
                        Id = id,
                        Text = text,
                        Source = item,
                        Disabled = disabled
                    });
                }
            });
        }
    }

    public abstract class LookupEditorBase<TItem> : LookupEditorBase<object, TItem>
        where TItem: class, new()
    {
        public LookupEditorBase(jQueryObject hidden)
            : base(hidden, null)
        {
        }
    }
}
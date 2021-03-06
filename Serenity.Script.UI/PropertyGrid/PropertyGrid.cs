﻿using jQueryApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Html;
using System.Linq;

namespace Serenity
{
    public class PropertyGrid : Widget<PropertyGridOptions>
    {
        private static JsDictionary<string, Type> KnownEditorTypes;

        static PropertyGrid()
        {
            KnownEditorTypes = new JsDictionary<string, Type>();
        }

        private List<Widget> editors;
        private List<PropertyItem> items;

        public PropertyGrid(jQueryObject div, PropertyGridOptions opt)
            : base(div, opt)
        {
            if (!Script.IsValue(opt.Mode))
                opt.Mode = PropertyGridMode.Insert;

            items = options.Items ?? new List<PropertyItem>();

            div.AddClass("s-PropertyGrid");

            editors = new List<Widget>();

            var categoryIndexes = new JsDictionary<string, int>();

            var categoriesDiv = div;

            if (options.UseCategories)
            {
                var linkContainer = J("<div/>")
                    .AddClass("category-links");

                categoryIndexes = CreateCategoryLinks(linkContainer, items);
                
                if (categoryIndexes.Count > 1)
                    linkContainer.AppendTo(div);
                else
                    linkContainer.Find("a.category-link").Unbind("click", CategoryLinkClick).Remove();

                categoriesDiv = J("<div/>")
                    .AddClass("categories")
                    .AppendTo(div);
            }
            
            var fieldContainer = categoriesDiv;

            string priorCategory = null;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (options.UseCategories &&
                    priorCategory != item.Category)
                {
                    var categoryDiv = CreateCategoryDiv(categoriesDiv, categoryIndexes, item.Category);

                    if (priorCategory == null)
                        categoryDiv.AddClass("first-category");

                    priorCategory = item.Category;
                    fieldContainer = categoryDiv;
                }

                var editor = CreateField(fieldContainer, item);

                editors[i] = editor;
            }

            UpdateReadOnly();
        }

        public override void Destroy()
        {
            if (editors != null)
            {
                for (var i = 0; i < editors.Count; i++)
                    editors[i].Destroy();
                editors = null;
            }

            this.element.Find("a.category-link").Unbind("click", CategoryLinkClick).Remove();

            base.Destroy();
        }

        private jQueryObject CreateCategoryDiv(jQueryObject categoriesDiv, JsDictionary<string, int> categoryIndexes, string category)
        {
            var categoryDiv = J("<div/>")
                .AddClass("category")
                .AppendTo(categoriesDiv);

            J("<div/>")
                .AddClass("category-title")
                .Append(J("<a/>")
                    .AddClass("category-anchor")
                    .Text(DetermineText(category, prefix => prefix + "Categories." + category))
                    .Attribute("name", options.IdPrefix + "Category" + categoryIndexes[category].ToString()))
                .AppendTo(categoryDiv);

            return categoryDiv;
        }

        private string DetermineText(string text, Func<string, string> getKey)
        {
            if (text != null &&
                !text.StartsWith("`"))
            {
                var local = Q.TryGetText(text);
                if (local != null)
                    return local;
            }

            if (text != null && text.StartsWith("`"))
                text = text.Substr(1);

            if (!options.LocalTextPrefix.IsEmptyOrNull())
            {
                var local = Q.TryGetText(getKey(options.LocalTextPrefix));
                if (local != null)
                    return local;
            }

            return text;
        }

        private Widget CreateField(jQueryObject container, PropertyItem item)
        {
            var fieldDiv = J("<div/>")
                .AddClass("field")
                .AddClass(item.Name)
                .Data("PropertyItem", item)
                .AppendTo(container);

            if (!String.IsNullOrEmpty(item.CssClass))
                fieldDiv.AddClass(item.CssClass);

            string editorId = options.IdPrefix + item.Name;

            string title = DetermineText(item.Title, prefix => prefix + item.Name);
            string hint = DetermineText(item.Hint, prefix => prefix + item.Name + "_Hint");
            string placeHolder = DetermineText(item.Placeholder, prefix => prefix + item.Name + "_Placeholder");

            var label = J("<label/>")
                .AddClass("caption")
                .Attribute("for", editorId)
                .Attribute("title", hint ?? title ?? "")
                .Html(title ?? "")
                .AppendTo(fieldDiv);

            if (item.Required)
                J("<sup>*</sup>")
                    .Attribute("title", Texts.Controls.PropertyGrid.RequiredHint)
                    .PrependTo(label);

            var editorType = EditorTypeRegistry.Get(item.EditorType);
            var elementAttr = editorType.GetCustomAttributes(typeof(ElementAttribute), true);
            string elementHtml = (elementAttr.Length > 0) ? elementAttr[0].As<ElementAttribute>().Html : "<input/>";

            var element = Widget.ElementFor(editorType)
                .AddClass("editor")
                .AddClass("flexify")
                .Attribute("id", editorId)
                .AppendTo(fieldDiv);

            if (element.Is(":input"))
                element.Attribute("name", item.Name ?? "");

            if (!placeHolder.IsEmptyOrNull())
                element.Attribute("placeholder", placeHolder);

            object editorParams = item.EditorParams;
            Type optionsType = null;
            var optionsAttr = editorType.GetCustomAttributes(typeof(OptionsTypeAttribute), true);
            if (optionsAttr != null && optionsAttr.Length > 0)
            {
                optionsType = optionsAttr[0].As<OptionsTypeAttribute>().OptionsType;
            }

            Widget editor;

            if (optionsType != null)
            {
                editorParams = jQuery.ExtendObject(Activator.CreateInstance(optionsType), item.EditorParams);

                editor = (Widget)(Activator.CreateInstance(editorType, element, editorParams));
            }
            else
            {
                editorParams = jQuery.ExtendObject(new object(), item.EditorParams);
                editor = (Widget)(Activator.CreateInstance(editorType, element, editorParams));
            }

            editor.Initialize();

            if (editor is BooleanEditor)
                label.RemoveAttr("for");

            if (Script.IsValue(item.MaxLength))
                SetMaxLength(editor, item.MaxLength.Value);

            if (item.EditorParams != null)
                ReflectionOptionsSetter.Set(editor, item.EditorParams);

            J("<div/>")
                .AddClass("vx")
                .AppendTo(fieldDiv);

            J("<div/>")
                .AddClass("clear")
                .AppendTo(fieldDiv);

            return editor;
        }

        private JsDictionary<string, int?> GetCategoryOrder(List<PropertyItem> items)
        {
            int order = 0;
            var result = new JsDictionary<string, int?>();

            var categoryOrder = options.CategoryOrder.TrimToNull();
            if (categoryOrder != null)
            {
                var split = categoryOrder.Split(";");
                foreach (var s in split)
                {
                    var x = s.TrimToNull();
                    if (x == null)
                        continue;

                    if (result[x] != null)
                        continue;

                    result[x] = order++;
                }
            }

            foreach (var x in items)
            {
                if (result[x.Category] == null)
                    result[x.Category] = order++;
            }

            return result;
        }

        private JsDictionary<string, int> CreateCategoryLinks(jQueryObject container, List<PropertyItem> items)
        {
            int idx = 0;
            var itemIndex = new JsDictionary<string, int>();
            foreach (var x in items)
            {
                x.Category = x.Category ?? options.DefaultCategory ?? "";
                itemIndex[x.Name] = idx++;
            }

            var self = this;
            var categoryOrder = GetCategoryOrder(items);
            
            items.Sort((x, y) => 
            {
                var c = 0;
                
                if (x.Category != y.Category)
                {
                    var c1 = categoryOrder[x.Category];
                    var c2 = categoryOrder[y.Category];
                    if (c1 != null && c2 != null)
                        c = c1.Value - c2.Value;
                    else if (c1 != null)
                        c = -1;
                    else if (c2 != null)
                        c = 1;
                }

                if (c == 0)
                    c = String.Compare(x.Category, y.Category);

                if (c == 0)
                    c = itemIndex[x.Name].CompareTo(itemIndex[y.Name]);

                return c;
            });

            var categoryIndexes = new JsDictionary<string, int>();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (!categoryIndexes.ContainsKey(item.Category))
                {
                    int index = categoryIndexes.Count + 1;
                    categoryIndexes[item.Category] = index;

                    if (index > 1)
                        J("<span/>")
                            .AddClass("separator")
                            .Text("|")
                            .PrependTo(container);
                    
                    J("<a/>")
                        .AddClass("category-link")
                        .Text(DetermineText(item.Category, prefix => prefix + "Categories." + item.Category))
                        .Attribute("tabindex", "-1")
                        .Attribute("href", "#" + options.IdPrefix + "Category" + index.ToString())
                        .Click(CategoryLinkClick)
                        .PrependTo(container);
                }
            }

            J("<div/>")
                .AddClass("clear")
                .AppendTo(container);

            return categoryIndexes;
        }

        private static void CategoryLinkClick(jQueryEvent e)
        {
            e.PreventDefault();

            var title = J("a[name=" + e.Target.GetAttribute("href").ToString().Substr(1) + "]");

            Action animate = delegate {
                title.FadeTo(100, 0.5, () => title.FadeTo(100, 1, () => { }));
            };

            var intoView = title.Closest(".category");
            if (intoView.Closest(":scrollable(both)").Length == 0)
                animate();
            else
                ((dynamic)intoView).scrollintoview(new {
                    duration = "fast",
                    direction = "y",
                    complete = animate
                });
        }

        public Widget[] Editors
        {
            get { return this.editors.As<Widget[]>(); }
        }

        public PropertyItem[] Items 
        { 
            get { return this.items.As<PropertyItem[]>(); }
        }

        public string IdPrefix
        {
            get { return options.IdPrefix; }
        }

        public PropertyGridMode Mode
        {
            get { return options.Mode; }
            set
            {
                if (options.Mode != value)
                {
                    options.Mode = value;
                    UpdateReadOnly();
                }
            }
        }

        public void Load(dynamic source)
        {
            for (var i = 0; i < editors.Count; i++)
            {
                var item = items[i];
                var editor = editors[i];

                if (Mode == PropertyGridMode.Insert &&
                    !Script.IsNullOrUndefined(item.DefaultValue) &&
                    Script.IsUndefined(source[item.Name]))
                    source[item.Name] = item.DefaultValue;

                LoadEditorValue(editor, item, source);
            }
        }

        public static void LoadEditorValue(Widget editor, PropertyItem item, dynamic source)
        {
            var setEditValue = editor as ISetEditValue;
            if (setEditValue != null)
                setEditValue.SetEditValue(source, item);

            var stringValue = editor as IStringValue;
            if (stringValue != null)
            {
                var value = source[item.Name];
                if (value != null)
                    value = value.toString();
                stringValue.Value = value;
            }
            else
            {
                var booleanValue = editor as IBooleanValue;
                if (booleanValue != null)
                {
                    object value = source[item.Name];
                    if (Script.TypeOf(value) == "number")
                        booleanValue.Value = IdExtensions.IsPositiveId(value.As<Int64>());
                    else
                        booleanValue.Value = Q.IsTrue(value);
                }
                else
                {
                    var doubleValue = editor as IDoubleValue;
                    if (doubleValue != null)
                    {
                        var d = source[item.Name];
                        if (d == null || (d is string && Q.IsTrimmedEmpty(d)))
                            doubleValue.Value = null;
                        else if (d is string)
                            doubleValue.Value = Q.ParseDecimal(d);
                        else if (d is Boolean)
                            doubleValue.Value = (Boolean)d ? 1 : 0;
                        else
                            doubleValue.Value = d;
                    }
                    else if (editor.Element.Is(":input"))
                    {
                        var v = source[item.Name];
                        if (!Script.IsValue(v))
                            editor.Element.Value("");
                        else
                            editor.Element.Value(((object)v).As<string>());
                    }
                }
            }
        }

        public void Save(dynamic target)
        {
            for (var i = 0; i < editors.Count; i++)
            {
                var item = items[i];
                if (!item.OneWay &&
                    !(Mode == PropertyGridMode.Insert && item.Insertable == false) &&
                    !(Mode == PropertyGridMode.Update && item.Updatable == false))
                {
                    var editor = editors[i];
                    SaveEditorValue(editor, item, target);
                }
            }
        }

        public static void SaveEditorValue(Widget editor, PropertyItem item, dynamic target)
        {
            var getEditValue = editor as IGetEditValue;
            if (getEditValue != null)
                getEditValue.GetEditValue(item, target);
            else
            {
                var stringValue = editor as IStringValue;
                if (stringValue != null)
                    target[item.Name] = stringValue.Value;
                else
                {
                    var booleanValue = editor as IBooleanValue;
                    if (booleanValue != null)
                        target[item.Name] = booleanValue.Value;
                    else
                    {
                        var doubleValue = editor as IDoubleValue;
                        if (doubleValue != null)
                        {
                            var value = doubleValue.Value;
                            target[item.Name] = Double.IsNaN(value.As<double>()) ? null : value;
                        }
                        else if (editor.Element.Is(":input"))
                        {
                            target[item.Name] = editor.Element.GetValue();
                        }
                    }
                }
            }
        }

        private void UpdateReadOnly()
        {
            for (var i = 0; i < editors.Count; i++)
            {
                var item = items[i];
                var editor = editors[i];

                bool readOnly = item.ReadOnly ||
                    (Mode == PropertyGridMode.Insert && item.Insertable == false) ||
                    (Mode == PropertyGridMode.Update && item.Updatable == false);

                SetReadOnly(editor, readOnly);
                SetRequired(editor, !readOnly && item.Required && 
                    (item.EditorType != "Boolean"));
            }
        }

        public void EnumerateItems(Action<PropertyItem, Widget> callback)
        {
            for (var i = 0; i < editors.Count; i++)
            {
                var item = items[i];
                var editor = editors[i];

                callback(item, editor);
            }
        }

        private static void SetMaxLength(Widget widget, int maxLength)
        {
            if (widget.Element.Is(":input"))
            {
                if (maxLength > 0)
                    widget.Element.Attribute("maxlength", maxLength.ToString());
                else
                    widget.Element.RemoveAttr("maxlength");
            }
        }

        public static void SetRequired(Widget widget, bool isRequired)
        {
            var req = widget as IValidateRequired;
            if (req != null)
                req.Required = isRequired;
            else if (widget.Element.Is(":input"))
                widget.Element.ToggleClass("required", Q.IsTrue(isRequired));

            var gridField = widget.GetGridField();
            var hasSupItem = gridField.Find("sup").GetItems().Any();
            if (isRequired && !hasSupItem){
                J("<sup>*</sup>")
                    .Attribute("title", "Bu alan zorunludur")
                    .PrependTo(gridField.Find(".caption")[0]);
            }
            else if (!isRequired && hasSupItem){
                J(gridField.Find("sup")[0]).Remove();
            }
        }

        public static void SetReadOnly(Widget widget, bool isReadOnly)
        {
            var readOnly = widget as IReadOnly;
            if (readOnly != null)
                readOnly.ReadOnly = isReadOnly;
            else if (widget.Element.Is(":input"))
                SetReadOnly(widget.Element, isReadOnly);
        }

        public static jQueryObject SetReadOnly(jQueryObject elements, bool isReadOnly)
        {
            elements.Each(delegate(int index, Element el)
            {
                jQueryObject elx = J(el);

                string type = elx.GetAttribute("type");

                if (elx.Is("select") || (type == "radio") || (type == "checkbox"))
                {
                    if (isReadOnly)
                    {
                        elx.AddClass("readonly").Attribute("disabled", "disabled");
                    }
                    else
                    {
                        elx.RemoveClass("readonly").RemoveAttr("disabled");
                    }
                }
                else
                {
                    if (isReadOnly)
                        elx.AddClass("readonly").Attribute("readonly", "readonly");
                    else
                        elx.RemoveClass("readonly").RemoveAttr("readonly");
                }

                return true;
            });

            return elements;
        }
    }
}
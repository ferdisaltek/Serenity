﻿using System;
using System.Runtime.CompilerServices;

namespace Serenity
{
    public class EditorAttribute : Attribute
    {
        public EditorAttribute()
        {
        }

        public string Key { get; set; }
    }

    public class OptionsTypeAttribute : Attribute
    {
        public OptionsTypeAttribute(Type optionsType)
        {
            OptionsType = optionsType;
        }

        public Type OptionsType { get; private set; }
    }
}
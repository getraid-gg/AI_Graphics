﻿using System.Reflection;

namespace AIGraphics
{
    internal static class ReflectionExtensions
    {
        internal static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        internal static void SetFieldValue<T>(this object obj, string name, object value)
        {
            // Set the flags so that private and public fields from instances will be found
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = obj.GetType().GetField(name, bindingFlags);
            field?.SetValue(obj, value);
        }
    }
}

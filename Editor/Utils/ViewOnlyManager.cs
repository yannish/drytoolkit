using System;
using System.Collections.Generic;
using System.Reflection;
using drytoolkit.Runtime.Utils;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    public static class ViewOnlyManager
    {
        private static readonly HashSet<int> _hiddenInstances = new HashSet<int>();

        public static bool IsVisible(int instanceId) => !_hiddenInstances.Contains(instanceId);

        public static void Toggle(int instanceId)
        {
            if (!_hiddenInstances.Remove(instanceId))
                _hiddenInstances.Add(instanceId);
        }

        public static bool HasViewOnlyFields(Type type)
        {
            while (type != null && type != typeof(MonoBehaviour) && type != typeof(ScriptableObject) && type != typeof(object))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    if (field.IsDefined(typeof(ViewOnlyAttribute), false))
                        return true;
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}

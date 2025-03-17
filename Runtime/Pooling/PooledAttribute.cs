using System;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class PooledAttribute : PropertyAttribute
{
    // public PooledAttribute(object target, string fieldName)
    // {
    //     FieldInfo field = target
    //         .GetType()
    //         .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //     
    //     if (field != null && !typeof(Component).IsAssignableFrom(field.FieldType))
    //     {
    //         throw new InvalidOperationException($"[ComponentOnly] attribute can only be used on Component fields! " +
    //                                             $"Field '{fieldName}' in '{target.GetType().Name}' is of type '{field.FieldType.Name}' which is not a Component.");
    //     }
    // }
}

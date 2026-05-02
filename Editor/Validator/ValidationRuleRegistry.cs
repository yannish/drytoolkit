using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace drytoolkit.Editor.Validator
{
    [InitializeOnLoad]
    internal static class ValidationRuleRegistry
    {
        internal sealed class RuleEntry
        {
            public Type   AssetType { get; }
            public string RuleName  { get; }
            public string Category  { get; }
            public bool   IsComponentRule { get; }  // true → scan prefabs + GetComponentsInChildren<T>

            // Avoids per-asset reflection overhead after initial discovery
            public Action<Object, string, List<ValidationIssue>> ValidateDelegate { get; }

            public RuleEntry(
                Type   assetType,
                string ruleName,
                string category,
                bool   isComponentRule,
                Action<Object, string, List<ValidationIssue>> validateDelegate)
            {
                AssetType       = assetType;
                RuleName        = ruleName;
                Category        = string.IsNullOrEmpty(category) ? "General" : category;
                IsComponentRule = isComponentRule;
                ValidateDelegate = validateDelegate;
            }
        }

        public static IReadOnlyList<RuleEntry> Rules { get; private set; } = Array.Empty<RuleEntry>();

        private static readonly Type _componentType     = typeof(Component);
        private static readonly Type _validationRuleOpen = typeof(IValidationRule<>);

        static ValidationRuleRegistry() => Refresh();

        internal static void Refresh()
        {
            var entries = new List<RuleEntry>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(assembly)) continue;

                Type[] types;
                try   { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;

                    var ruleInterface = FindValidationRuleInterface(type);
                    if (ruleInterface == null) continue;

                    var assetType = ruleInterface.GetGenericArguments()[0];

                    IValidationRule instance;
                    try   { instance = (IValidationRule)Activator.CreateInstance(type); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Validator] Could not instantiate rule '{type.FullName}': {e.Message}");
                        continue;
                    }

                    var ruleName = (string)ruleInterface.GetProperty("RuleName").GetValue(instance);
                    var category = (string)ruleInterface.GetProperty("Category").GetValue(instance);
                    var validateMethod = ruleInterface.GetMethod("Validate");
                    bool isComponent = assetType.IsSubclassOf(_componentType);

                    Action<Object, string, List<ValidationIssue>> del = (obj, path, issues) =>
                        validateMethod.Invoke(instance, new object[] { obj, path, issues });

                    entries.Add(new RuleEntry(assetType, ruleName, category, isComponent, del));
                }
            }

            Rules = entries;
        }

        private static Type FindValidationRuleInterface(Type type)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == _validationRuleOpen)
                    return iface;
            }
            return null;
        }

        private static bool ShouldSkipAssembly(Assembly asm)
        {
            var name = asm.GetName().Name;
            return name.StartsWith("Unity",       StringComparison.Ordinal)
                || name.StartsWith("System",      StringComparison.Ordinal)
                || name.StartsWith("mscorlib",    StringComparison.Ordinal)
                || name.StartsWith("Mono.",       StringComparison.Ordinal)
                || name.StartsWith("netstandard", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.",  StringComparison.Ordinal)
                || name.StartsWith("nunit.",      StringComparison.Ordinal)
                || name.StartsWith("log4net",     StringComparison.Ordinal)
                || name.StartsWith("ExCSS",       StringComparison.Ordinal);
        }
    }
}

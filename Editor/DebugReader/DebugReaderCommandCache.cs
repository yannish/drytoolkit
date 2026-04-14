using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Scans all user assemblies for [DebugCommand] methods once per domain reload
// and caches the results. The inspector reads only from this cache — no per-frame reflection.
[InitializeOnLoad]
public static class DebugReaderCommandCache
{
    // key (e.g. "Climbing.ResetState") → method to invoke
    public static readonly Dictionary<string, MethodInfo> Commands = new();

    static DebugReaderCommandCache()
    {
        Commands.Clear();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (ShouldSkipAssembly(assembly)) continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Some assemblies fail GetTypes() — use whatever loaded successfully
                types = e.Types;
            }

            foreach (var type in types)
            {
                if (type == null) continue;

                foreach (var method in type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = method.GetCustomAttribute<DebugCommandAttribute>();
                    if (attr == null) continue;

                    if (method.GetParameters().Length > 0)
                    {
                        Debug.LogWarning($"[DebugCommand] '{type.FullName}.{method.Name}' has parameters and will be skipped — debug commands must be parameterless.");
                        continue;
                    }

                    if (Commands.ContainsKey(attr.Key))
                    {
                        Debug.LogWarning($"[DebugCommand] Duplicate key '{attr.Key}' found on '{type.FullName}.{method.Name}' — earlier registration kept.");
                        continue;
                    }

                    Commands[attr.Key] = method;
                }
            }
        }
    }

    private static bool ShouldSkipAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        return name.StartsWith("Unity", StringComparison.Ordinal)
            || name.StartsWith("System", StringComparison.Ordinal)
            || name.StartsWith("mscorlib", StringComparison.Ordinal)
            || name.StartsWith("Mono.", StringComparison.Ordinal)
            || name.StartsWith("netstandard", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal)
            || name.StartsWith("nunit.", StringComparison.Ordinal)
            || name.StartsWith("log4net", StringComparison.Ordinal)
            || name.StartsWith("ExCSS", StringComparison.Ordinal);
    }
}

using System;

/// <summary>
/// Marks a static method as a debug command callable from the DebugSettingsRegistry inspector.
/// The key follows the same Group.CommandName convention as other debug settings.
/// Methods must be static and take no parameters.
/// </summary>
/// <example>
/// [DebugCommand("Climbing.ResetState")]
/// public static void ResetClimbingState() { ... }
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class DebugCommandAttribute : Attribute
{
    public string Key { get; }

    public DebugCommandAttribute(string key)
    {
        Key = key;
    }
}

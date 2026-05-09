using System;

namespace drytoolkit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InspectableAttribute : Attribute { }
}

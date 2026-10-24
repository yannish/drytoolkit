using System;

namespace drytoolkit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InspectableAttribute : Attribute
    {
        /// <summary>
        /// Required for arrays and lists: name of an int field, property or zero-arg method
        /// holding how many leading elements are valid (e.g. the return value of a NonAlloc call).
        /// </summary>
        public readonly string countFrom;

        public InspectableAttribute(string countFrom = null)
        {
            this.countFrom = countFrom;
        }
    }
}

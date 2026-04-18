using System;
using UnityEngine;

namespace drytoolkit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldAttribute : PropertyAttribute
    {
        public readonly string title;
        public readonly string titleFrom;

        public FoldoutGroupAttribute(string title, string titleFrom = null)
        {
            this.title = title;
            this.titleFrom = titleFrom;
        }
    }
}

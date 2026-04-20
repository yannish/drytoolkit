using System;

namespace drytoolkit.Runtime.Utils
{
    public enum ButtonPosition { Top, Bottom }

    [AttributeUsage(AttributeTargets.Method)]
    public class EditorButtonAttribute : Attribute
    {
        public readonly ButtonPosition position;
        public readonly string group;
        public readonly string label;

        public EditorButtonAttribute(
            ButtonPosition position = ButtonPosition.Bottom,
            string group = null,
            string label = null)
        {
            this.position = position;
            this.group = group;
            this.label = label;
        }
    }
}

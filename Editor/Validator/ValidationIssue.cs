using UnityEngine;

namespace drytoolkit.Editor.Validator
{
    public enum ValidationSeverity { Warning, Error }

    public sealed class ValidationIssue
    {
        public string             Message;
        public ValidationSeverity Severity;
        public Object             Context;   // asset or component — both pingable via EditorGUIUtility.PingObject
        public string             AssetPath;
        public string             RuleName;
        public string             Category;
    }
}

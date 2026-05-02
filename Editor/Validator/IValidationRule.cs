using System.Collections.Generic;
using UnityEngine;

namespace drytoolkit.Editor.Validator
{
    public interface IValidationRule { }

    public interface IValidationRule<T> : IValidationRule where T : Object
    {
        string RuleName { get; }
        string Category { get; }
        void Validate(T target, string assetPath, List<ValidationIssue> issues);
    }
}

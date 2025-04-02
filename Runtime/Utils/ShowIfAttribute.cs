using UnityEngine;

public class ShowIfAttribute : PropertyAttribute
{
    public string conditionField; // Name of the boolean field to check
    public object compareValue;   // Optional: Value to compare against

    public ShowIfAttribute(string conditionField, object compareValue = null)
    {
        this.conditionField = conditionField;
        this.compareValue = compareValue;
    }
}
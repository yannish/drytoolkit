using UnityEngine;

public class EnableIfAttribute : PropertyAttribute
{
    public string conditionField; // Name of the boolean field to check
    public object compareValue;   // Optional: Value to compare against

    public EnableIfAttribute(string conditionField, object compareValue = null)
    {
        this.conditionField = conditionField;
        this.compareValue = compareValue;
    }
}

using System;
using UnityEngine;

[Serializable]
public class ColorReference
{
    public bool UseConstant = true;
    public Color ConstantColor = new Color(0f, 0f, 0f, 1f);
    public ColorVariable ColorVariable;

    public ColorReference() { }

    public Color ColorValue
    {
        get { return (UseConstant || ColorVariable == null) ? ConstantColor : ColorVariable.color; }
    }

    public ColorReference(Color color)
    {
        UseConstant = true;
        ConstantColor = color;
    }

    public static implicit operator Color(ColorReference reference)
    {
        return reference.ColorValue;
    }
}
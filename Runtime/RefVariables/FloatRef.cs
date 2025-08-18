using System;
using UnityEngine;

[Serializable]
public class FloatRef
{
    public bool useConstant = false;

    public float constantValue;

    public FloatVar variable;
    
    public FloatRef(){}

    public FloatRef(FloatVar variable)
    {
        this.variable = variable;
        useConstant = false;
    }

    public FloatRef(float value)
    {
        useConstant = true;
        constantValue = value;
    }
    
    public float Value => useConstant || variable == null ? constantValue : variable.value;
    
    public static implicit operator float(FloatRef reference) => reference.Value;
}

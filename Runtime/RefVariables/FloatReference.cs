using System;

[Serializable]
public class FloatReference
{
    public bool UseConstant = false;
    
    public float ConstantValue;
    
    public FloatVariable Variable;

    public FloatReference() { }

    public FloatReference(FloatVariable variable)
    {
        Variable = variable;
        UseConstant = false;
    }

    public FloatReference(float value)
    {
        UseConstant = true;
        ConstantValue = value;
    }

    public float Value => UseConstant || Variable == null ? ConstantValue : Variable.Value;

    public static implicit operator float(FloatReference reference) => reference.Value;
}
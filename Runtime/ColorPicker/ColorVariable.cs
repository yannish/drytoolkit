using UnityEngine;

[CreateAssetMenu(menuName = "RefVariable/ColorVariable", fileName = "colorVariable")]
public class ColorVariable : ScriptableObject
{
    public Color color;

    public void SetColor(Color color) => this.color = color;

    public void SetColor(ColorVariable colorVariable) => this.color = colorVariable.color;
}
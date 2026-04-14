using UnityEngine;

public abstract class DebugReaderSettingBase : ScriptableObject
{
    public string GroupName
    {
        get
        {
            int dot = name.IndexOf('.');
            return dot > 0 ? name.Substring(0, dot) : "Ungrouped";
        }
    }

    public string SettingName
    {
        get
        {
            int dot = name.IndexOf('.');
            return dot > 0 ? name.Substring(dot + 1) : name;
        }
    }

    public string FullKey => name;
}

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Debug Settings/Registry", fileName = "DebugSettingsRegistry")]
public class DebugSettingsRegistry : ScriptableObject
{
    [Serializable]
    public struct GroupState
    {
        public string groupName;
        public bool muted;
        public bool foldoutOpen;
    }

    [Tooltip("When enabled, DebugReader.cs is regenerated (and scripts recompile) automatically each time a setting asset is created or deleted. " +
             "Disable when creating many settings in a row to avoid recompiling between each — then hit Refresh when done.")]
    public bool autoRefresh = true;

    [SerializeField] private List<DebugSettingBase> _settings = new();
    [SerializeField] private List<GroupState> _groupStates = new();

    public IReadOnlyList<DebugSettingBase> Settings => _settings;

    public void SetSettings(List<DebugSettingBase> settings)
    {
        _settings = settings;
        SyncGroupStates();
    }

    private void SyncGroupStates()
    {
        var knownGroups = new HashSet<string>();
        foreach (var s in _settings)
            if (s != null) knownGroups.Add(s.GroupName);

        _groupStates.RemoveAll(g => !knownGroups.Contains(g.groupName));

        foreach (var group in knownGroups)
        {
            if (!_groupStates.Exists(gs => gs.groupName == group))
                _groupStates.Add(new GroupState { groupName = group, muted = false, foldoutOpen = true });
        }
    }

    public bool IsGroupMuted(string groupName)
    {
        foreach (var gs in _groupStates)
            if (gs.groupName == groupName) return gs.muted;
        return false;
    }

    public void SetGroupMuted(string groupName, bool muted)
    {
        for (int i = 0; i < _groupStates.Count; i++)
        {
            if (_groupStates[i].groupName != groupName) continue;
            var gs = _groupStates[i];
            gs.muted = muted;
            _groupStates[i] = gs;
            return;
        }
    }

    public bool GetGroupFoldout(string groupName)
    {
        foreach (var gs in _groupStates)
            if (gs.groupName == groupName) return gs.foldoutOpen;
        return true;
    }

    public void SetGroupFoldout(string groupName, bool open)
    {
        for (int i = 0; i < _groupStates.Count; i++)
        {
            if (_groupStates[i].groupName != groupName) continue;
            var gs = _groupStates[i];
            gs.foldoutOpen = open;
            _groupStates[i] = gs;
            return;
        }
    }

    public DebugSettingBase GetSetting(string fullKey)
    {
        foreach (var s in _settings)
            if (s != null && s.FullKey == fullKey) return s;
        return null;
    }

    public List<GroupState> GetAllGroupStates() => _groupStates;
}

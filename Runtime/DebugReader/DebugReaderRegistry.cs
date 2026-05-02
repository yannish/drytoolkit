using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Debug Reader/Registry", fileName = "DebugReaderRegistry")]
public class DebugReaderRegistry : ScriptableObject
{
    [Serializable]
    public struct GroupState
    {
        public string groupName;
        public bool muted;
        public bool foldoutOpen;
        public bool pinned;
    }

    [SerializeField] private List<DebugReaderSettingBase> _settings = new();
    [SerializeField] private List<GroupState> _groupStates = new();

    public IReadOnlyList<DebugReaderSettingBase> Settings => _settings;

    public void SetSettings(List<DebugReaderSettingBase> settings)
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
        _groupStates.Add(new GroupState { groupName = groupName, muted = muted, foldoutOpen = true });
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
        _groupStates.Add(new GroupState { groupName = groupName, muted = false, foldoutOpen = open });
    }

    public bool IsGroupPinned(string groupName)
    {
        foreach (var gs in _groupStates)
            if (gs.groupName == groupName) return gs.pinned;
        return false;
    }

    public void SetGroupPinned(string groupName, bool pinned)
    {
        for (int i = 0; i < _groupStates.Count; i++)
        {
            if (_groupStates[i].groupName != groupName) continue;
            var gs = _groupStates[i];
            gs.pinned = pinned;
            _groupStates[i] = gs;
            return;
        }
        _groupStates.Add(new GroupState { groupName = groupName, muted = false, foldoutOpen = true, pinned = pinned });
    }

    public DebugReaderSettingBase GetSetting(string fullKey)
    {
        foreach (var s in _settings)
            if (s != null && s.FullKey == fullKey) return s;
        return null;
    }

    public List<GroupState> GetAllGroupStates() => _groupStates;
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace drytoolkit.Editor.Validator
{
    internal sealed class ValidatorRunner
    {
        // Assets (or component instances) processed per EditorApplication.update tick.
        // Conservative default — tune via the constant if needed.
        private const int ItemsPerFrame = 10;

        // ── Public state ───────────────────────────────────────────────────────

        public bool   IsRunning       { get; private set; }
        public int    TotalItems      { get; private set; }
        public int    ProcessedItems  { get; private set; }
        public string CurrentRuleName { get; private set; } = "";
        public IReadOnlyList<ValidationIssue> Issues => _issues;

        public event Action OnCompleted;
        public event Action OnProgress;

        // ── Private state ──────────────────────────────────────────────────────

        private readonly List<ValidationIssue> _issues = new();

        private struct WorkItem
        {
            public ValidationRuleRegistry.RuleEntry Rule;
            public Object                           Target;    // asset or extracted component
            public string                           AssetPath;
        }

        private List<WorkItem> _workItems;
        private int            _workIndex;
        private int            _progressId = -1;

        // ── Public API ─────────────────────────────────────────────────────────

        public void Start()
        {
            if (IsRunning) Stop();

            _issues.Clear();
            _workItems      = BuildWorkList();
            _workIndex      = 0;
            TotalItems      = _workItems.Count;
            ProcessedItems  = 0;
            IsRunning       = true;

            _progressId = Progress.Start(
                "drytoolkit Validator",
                "Building work list...",
                Progress.Options.Managed);

            EditorApplication.update += Tick;
        }

        public void Stop()
        {
            EditorApplication.update -= Tick;
            IsRunning = false;

            if (_progressId >= 0)
            {
                Progress.Remove(_progressId);
                _progressId = -1;
            }
        }

        // ── Tick ───────────────────────────────────────────────────────────────

        private void Tick()
        {
            if (!IsRunning) return;

            int end = Mathf.Min(_workIndex + ItemsPerFrame, _workItems.Count);
            for (int i = _workIndex; i < end; i++)
                ProcessItem(_workItems[i]);

            _workIndex     = end;
            ProcessedItems = end;
            CurrentRuleName = end < _workItems.Count ? _workItems[end].Rule.RuleName : "";

            float pct = TotalItems > 0 ? (float)end / TotalItems : 1f;
            if (_progressId >= 0)
                Progress.Report(_progressId, pct, $"Scanning: {CurrentRuleName}");

            OnProgress?.Invoke();

            if (end >= _workItems.Count)
            {
                Stop();
                OnCompleted?.Invoke();
            }
        }

        private void ProcessItem(WorkItem item)
        {
            try
            {
                item.Rule.ValidateDelegate(item.Target, item.AssetPath, _issues);
            }
            catch (Exception e)
            {
                _issues.Add(new ValidationIssue
                {
                    Message   = $"Rule '{item.Rule.RuleName}' threw an exception: {e.Message}",
                    Severity  = ValidationSeverity.Error,
                    Context   = item.Target,
                    AssetPath = item.AssetPath,
                    RuleName  = item.Rule.RuleName,
                    Category  = item.Rule.Category,
                });
            }
        }

        // ── Work list construction ─────────────────────────────────────────────

        private static List<WorkItem> BuildWorkList()
        {
            var items = new List<WorkItem>();

            // Prefab paths are shared across all component rules — build once
            string[] prefabGuids = null;

            foreach (var rule in ValidationRuleRegistry.Rules)
            {
                if (rule.IsComponentRule)
                {
                    prefabGuids ??= AssetDatabase.FindAssets("t:Prefab");

                    foreach (var guid in prefabGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;

                        var components = prefab.GetComponentsInChildren(rule.AssetType, includeInactive: true);
                        foreach (var c in components)
                        {
                            items.Add(new WorkItem { Rule = rule, Target = c, AssetPath = path });
                        }
                    }
                }
                else
                {
                    var guids = AssetDatabase.FindAssets($"t:{rule.AssetType.Name}");
                    foreach (var guid in guids)
                    {
                        var path   = AssetDatabase.GUIDToAssetPath(guid);
                        var asset  = AssetDatabase.LoadAssetAtPath(path, rule.AssetType);
                        if (asset == null) continue;  // type filter false positive

                        items.Add(new WorkItem { Rule = rule, Target = asset, AssetPath = path });
                    }
                }
            }

            return items;
        }
    }
}

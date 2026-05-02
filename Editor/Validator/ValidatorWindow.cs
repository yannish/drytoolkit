using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Validator
{
    internal sealed class ValidatorWindow : EditorWindow
    {
        // ── State ──────────────────────────────────────────────────────────────

        private ValidatorRunner _runner;
        private Vector2         _scrollPos;
        private string          _searchQuery    = "";
        private GroupMode       _groupMode      = GroupMode.BySeverity;
        private SeverityFilter  _severityFilter = SeverityFilter.All;

        private List<ValidationIssue> _filteredIssues = new();
        private bool                  _resultsDirty   = true;

        private enum GroupMode     { BySeverity, ByCategory, ByRule, Flat }
        private enum SeverityFilter { All, ErrorsOnly, WarningsOnly }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        [MenuItem("Tools/drytoolkit/Validator")]
        public static void Open()
        {
            var w = GetWindow<ValidatorWindow>("Validator");
            w.minSize = new Vector2(440, 320);
        }

        private void OnEnable()
        {
            _runner              = new ValidatorRunner();
            _runner.OnProgress  += OnRunnerTick;
            _runner.OnCompleted += OnRunnerTick;
        }

        private void OnDisable()
        {
            _runner?.Stop();
            _runner = null;
        }

        private void OnRunnerTick() { _resultsDirty = true; Repaint(); }

        // ── OnGUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawToolbar();
            DrawStatusBar();
            DrawDivider();
            RebuildIfDirty();
            DrawIssueList();
        }

        // ── Toolbar ────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_runner.IsRunning)
                {
                    if (GUILayout.Button("Stop", GUILayout.Width(60)))
                        _runner.Stop();
                }
                else
                {
                    if (GUILayout.Button("Run All", GUILayout.Width(60)))
                    {
                        _resultsDirty = true;
                        _runner.Start();
                    }
                }

                GUILayout.Space(6);

                EditorGUI.BeginChangeCheck();
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck()) _resultsDirty = true;

                GUILayout.Space(6);

                EditorGUILayout.LabelField("Show:", GUILayout.Width(38));
                EditorGUI.BeginChangeCheck();
                _severityFilter = (SeverityFilter)EditorGUILayout.EnumPopup(_severityFilter, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck()) _resultsDirty = true;

                GUILayout.Space(6);

                EditorGUILayout.LabelField("Group:", GUILayout.Width(44));
                EditorGUI.BeginChangeCheck();
                _groupMode = (GroupMode)EditorGUILayout.EnumPopup(_groupMode, GUILayout.Width(90));
                if (EditorGUI.EndChangeCheck()) _resultsDirty = true;
            }
            EditorGUILayout.Space(4);
        }

        // ── Status / progress bar ──────────────────────────────────────────────

        private void DrawStatusBar()
        {
            if (_runner.IsRunning)
            {
                float pct = _runner.TotalItems > 0
                    ? (float)_runner.ProcessedItems / _runner.TotalItems
                    : 0f;
                var rect = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.ProgressBar(rect, pct,
                    $"Scanning {_runner.CurrentRuleName}… {_runner.ProcessedItems}/{_runner.TotalItems}");
            }
            else
            {
                int errors   = _runner.Issues.Count(i => i.Severity == ValidationSeverity.Error);
                int warnings = _runner.Issues.Count(i => i.Severity == ValidationSeverity.Warning);

                if (_runner.Issues.Count == 0)
                    EditorGUILayout.LabelField("No results — press Run All to scan.", EditorStyles.centeredGreyMiniLabel);
                else
                    EditorGUILayout.LabelField(
                        $"{errors} error(s)   {warnings} warning(s)   ({_filteredIssues.Count} visible)",
                        errors > 0 ? EditorStyles.boldLabel : EditorStyles.label);
            }
        }

        // ── Issue list ─────────────────────────────────────────────────────────

        private void RebuildIfDirty()
        {
            if (!_resultsDirty) return;
            _resultsDirty = false;

            var query    = _searchQuery?.ToLowerInvariant();
            bool hasQuery = !string.IsNullOrEmpty(query);

            _filteredIssues = _runner.Issues.Where(issue =>
            {
                if (_severityFilter == SeverityFilter.ErrorsOnly   && issue.Severity != ValidationSeverity.Error)   return false;
                if (_severityFilter == SeverityFilter.WarningsOnly  && issue.Severity != ValidationSeverity.Warning) return false;
                if (hasQuery)
                    return issue.Message.ToLowerInvariant().Contains(query)
                        || issue.AssetPath.ToLowerInvariant().Contains(query)
                        || issue.RuleName.ToLowerInvariant().Contains(query);
                return true;
            }).ToList();
        }

        private void DrawIssueList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_filteredIssues.Count == 0 && !_runner.IsRunning)
            {
                EditorGUILayout.HelpBox(
                    _runner.Issues.Count == 0
                        ? "Run the validator to see results."
                        : $"No issues match the current filter.",
                    MessageType.Info);
            }
            else
            {
                switch (_groupMode)
                {
                    case GroupMode.BySeverity: DrawGrouped(i => i.Severity.ToString()); break;
                    case GroupMode.ByCategory: DrawGrouped(i => i.Category);            break;
                    case GroupMode.ByRule:     DrawGrouped(i => i.RuleName);            break;
                    case GroupMode.Flat:       DrawFlat();                               break;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGrouped(Func<ValidationIssue, string> keySelector)
        {
            foreach (var group in _filteredIssues.GroupBy(keySelector).OrderBy(g => g.Key))
            {
                int e = group.Count(i => i.Severity == ValidationSeverity.Error);
                int w = group.Count(i => i.Severity == ValidationSeverity.Warning);

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"{group.Key}  [{e}E / {w}W]", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var issue in group)
                        DrawIssueRow(issue);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawFlat()
        {
            foreach (var issue in _filteredIssues)
                DrawIssueRow(issue);
        }

        private static readonly GUIContent _errorIcon   = EditorGUIUtility.IconContent("console.erroricon.sml");
        private static readonly GUIContent _warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");

        private void DrawIssueRow(ValidationIssue issue)
        {
            var icon = issue.Severity == ValidationSeverity.Error ? _errorIcon : _warningIcon;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(icon, GUILayout.Width(18), GUILayout.Height(18));

                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(issue.AssetPath, EditorStyles.miniLabel);
                }

                if (issue.Context != null
                    && GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(38)))
                {
                    EditorGUIUtility.PingObject(issue.Context);
                    Selection.activeObject = issue.Context;
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void DrawDivider()
        {
            EditorGUILayout.Space(2);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 0.35f));
            EditorGUILayout.Space(2);
        }
    }
}

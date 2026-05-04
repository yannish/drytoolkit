using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DebugReaderCreateWindow : EditorWindow
{
    private Type   _settingType;
    private string _registryFolder;
    private DebugReaderRegistry _registry;

    private string _inputName  = "";
    private bool   _focusDone  = false;

    public static void Show(Type settingType, string registryFolder, DebugReaderRegistry registry, string prefill = "")
    {
        var window = CreateInstance<DebugReaderCreateWindow>();
        window._settingType     = settingType;
        window._registryFolder  = registryFolder;
        window._registry        = registry;
        window._inputName       = prefill;
        window.titleContent     = new GUIContent($"New Debug {settingType.Name.Replace("DebugReader", "")}");
        window.minSize          = new Vector2(320, 80);
        window.maxSize          = new Vector2(480, 80);
        window.ShowAuxWindow();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        GUI.SetNextControlName("NameField");
        _inputName = EditorGUILayout.TextField("Name", _inputName);

        // Auto-focus the text field on the first couple of frames
        if (!_focusDone)
        {
            EditorGUI.FocusTextInControl("NameField");
            _focusDone = true;
            Repaint();
        }

        EditorGUILayout.Space(4);

        bool valid = IsValid(_inputName);

        HandleKeyboard(valid);

        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button("Create"))
                Confirm();
        }

        if (!valid && _inputName.Length > 0)
        {
            EditorGUILayout.HelpBox(
                "Use the format  Group.SettingName  (one dot, no spaces, both parts non-empty).",
                MessageType.Warning);
            minSize = new Vector2(320, 140);
            maxSize = new Vector2(480, 140);
        }
        else
        {
            minSize = new Vector2(320, 80);
            maxSize = new Vector2(480, 80);
        }
    }

    private void HandleKeyboard(bool valid)
    {
        var e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Escape)
        {
            Close();
            e.Use();
        }
        else if (valid && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            Confirm();
            e.Use();
        }
    }

    private void Confirm()
    {
        CreateSettingAsset(_inputName, _settingType, _registryFolder, _registry);
        Close();
    }

    private static bool IsValid(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        int dot = name.IndexOf('.');
        if (dot <= 0 || dot == name.Length - 1) return false;
        if (name.IndexOf(' ') >= 0) return false;
        return true;
    }

    private static void CreateSettingAsset(
        string fullName,
        Type settingType,
        string registryFolder,
        DebugReaderRegistry registry)
    {
        int dot       = fullName.IndexOf('.');
        string group  = fullName.Substring(0, dot);
        string folder = $"{registryFolder}/{group}";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(registryFolder, group);

        string assetPath = $"{folder}/{fullName}.asset";

        if (AssetDatabase.AssetPathExists(assetPath))
        {
            EditorUtility.DisplayDialog(
                "Already Exists",
                $"An asset named '{fullName}' already exists in {folder}.",
                "OK");
            return;
        }

        var instance = CreateInstance(settingType);
        AssetDatabase.CreateAsset(instance, assetPath);
        AssetDatabase.SaveAssets();

        // Ping the new asset so the user can see it
        EditorGUIUtility.PingObject(instance);
    }
}

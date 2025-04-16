using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

// [CustomPropertyDrawer(typeof(ExpandableAttribute))]
public class ExpandableAttributePropertyDrawer_OLD : PropertyDrawer
{
    [InitializeOnLoadMethod]
    public static void ExpandableCleanupHandler()
    {
        Selection.selectionChanged -= CleanupExpandedEditors;
        Selection.selectionChanged += CleanupExpandedEditors;
    }

    public class InsetEditorCache
    {
        public Editor cachedEditor;
        public AnimBool animBool;
        public SerializedProperty prop;
    }
    
    private static void CleanupExpandedEditors()
    {
        Debug.LogWarning("Cleaning up expanded editors.");

        // foreach (var kvp in editorCacheLookup)
        // {
        //     kvp.Value.animBool.valueChanged.RemoveAllListeners();
        //     kvp.Value.animBool.value = false;
        //     kvp.Value.animBool.target = false;
        //     // kvp.Value.animBool.
        //     // kvp.Value.prop.isExpanded = false;
        //     Editor.DestroyImmediate(kvp.Value.cachedEditor);
        // }
        //
        // foreach (var kvp in idToAnimBools)
        // {
        //     kvp.Value.target = false;
        // }
        // idToAnimBools.Clear();
    }

    private AnimBool _animBool;
    
    public static Dictionary<int, InsetEditorCache> editorCacheLookup = new Dictionary<int, InsetEditorCache>();
    
    // public static Dictionary<int, SerializedProperty> idTorProperties = new Dictionary<int, SerializedProperty>();
    //
    // public static Dictionary<int, AnimBool> idToAnimBools = new Dictionary<int, AnimBool>();
    //
    // public static Dictionary<int, Editor> instanceIDtoEditorLookup = new Dictionary<int, Editor>();
    
    public static List<int> instanceIdsDrawing = new List<int>();

    public static void TickEditorsLookup()
    {
        
    }

    private Editor _editor = null;

    // private static SerializedProperty currExpandedProp;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label, true);
        if (property.objectReferenceValue == null)
            return;
        
        if (_animBool == null)
        {
            _animBool = new AnimBool(property.isExpanded);
            _animBool.valueChanged.AddListener(() =>
            {
                EditorWindow.focusedWindow.Repaint();
                if (_animBool.faded == 0f)
                {
                    Object.DestroyImmediate(_editor);
                }
            });
        }

        EditorGUI.BeginChangeCheck();
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
        _animBool.target = property.isExpanded;
        if (EditorGUI.EndChangeCheck())
        {
            if (property.isExpanded)
            {
                Debug.LogWarning("Expanded an inset-editor.");
                // idToAnimBools.Add(property.objectReferenceValue.GetInstanceID(), _animBool);
            }
        }

        if (EditorGUILayout.BeginFadeGroup(_animBool.faded))
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                int instanceId = property.objectReferenceValue.GetInstanceID();
                if (!editorCacheLookup.TryGetValue(instanceId, out InsetEditorCache cache))
                {
                    var newEditor = Editor.CreateEditor(property.objectReferenceValue, null);
                    editorCacheLookup.Add(instanceId, new InsetEditorCache()
                    {
                        cachedEditor = newEditor,
                        animBool = _animBool,
                        prop = property
                    });
                }
                
                // if (!_editor)
                // {
                //     Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                // }
                cache.cachedEditor.OnInspectorGUI();
                // _editor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUI.EndProperty();
    }

    private static bool refreshQueued = false;
    public static void QueueRefresh()
    {
        if (refreshQueued)
            return;

        refreshQueued = true;
        EditorApplication.delayCall += DoRefresh;
    }

    private static void DoRefresh()
    {
        Debug.Log("Doing refresh.");
        foreach(var value in instanceIdsDrawing)
            Debug.LogWarning($"drew {value} this frame");
        instanceIdsDrawing.Clear();
        refreshQueued = false;
    }
}

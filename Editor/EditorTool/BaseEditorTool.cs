using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

public abstract class BaseEditorTool : EditorTool
{
    const string iconPath = "Assets/Icons/ToolIcons/";

    protected virtual string iconName { get; }

    protected virtual void FetchIcon() => m_ToolIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + iconName);

    public override GUIContent toolbarIcon => m_IconContent;

    public string displayName
    {
        get => m_DisplayName;
        set => m_DisplayName = value;
    }

    public string tooltip
    {
        get => m_Tooltip;
        set => m_Tooltip = value;
    }

    public Vector3 cursorPosition => m_CursorPosition;

    internal virtual EventModifiers WhitelistModifiers => EventModifiers.None;


#pragma warning disable CS0649
    [SerializeField]
    Texture2D m_ToolIcon;
#pragma warning restore CS0649
    string m_DisplayName;
    string m_Tooltip;
    GUIContent m_IconContent;
    Vector3 m_CursorPosition;

    private void OnEnable()
    {
        FetchIcon();

        // Update Icon
        m_IconContent = new GUIContent()
        {
            image = m_ToolIcon,
            text = m_DisplayName,
            tooltip = m_Tooltip,
        };

        // Callbacks
        Undo.undoRedoPerformed += OnUndo;
    }

    protected Vector3 GetCurrentMousePositionInScene()
    {
        Vector3 mousePosition = Event.current.mousePosition;
        var placeObject = HandleUtility.PlaceObject(mousePosition, out var newPosition, out var normal);
        return placeObject ? newPosition : HandleUtility.GUIPointToWorldRay(mousePosition).GetPoint(10);
    }
    

    private void OnDestroy()
    {
        // Callbacks
        Undo.undoRedoPerformed -= OnUndo;
    }
    

    public override void OnToolGUI(EditorWindow window)
    {
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0);
        }

        using (new Handles.DrawingScope(Color.white))
        {
            DrawHandles();
        }

        Event evt = Event.current;

        // Only handle mouse input if either:
        // A. No modifiers are pressed
        // B. Specific modifiers are pressed. (Override WhitelistModifiers per tool type)
        if (evt.modifiers == EventModifiers.None || (evt.modifiers & WhitelistModifiers) != 0)
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    OnMouseDown();
                    break;
                case EventType.MouseUp:
                    OnMouseUp();
                    break;
                case EventType.MouseDrag:
                    OnMouseDrag();
                    break;
                case EventType.MouseLeaveWindow:
                    OnMouseLeaveWindow();
                    break;
            }
        }

        LateTick();

        window.Repaint();
    }
    
    
    public abstract void DrawHandles();

    public virtual void LateTick() { }

    public virtual void OnMouseDown() { }

    public virtual void OnMouseUp() { }

    public virtual void OnMouseDrag() { }

    public virtual void OnMouseLeaveWindow() { }

    public virtual void OnUndo() { }
}


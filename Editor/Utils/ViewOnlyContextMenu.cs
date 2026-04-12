using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    public static class ViewOnlyContextMenu
    {
        private const string MonoBehaviourMenuPath = "CONTEXT/MonoBehaviour/ViewOnly Fields";
        private const string ScriptableObjectMenuPath = "CONTEXT/ScriptableObject/ViewOnly Fields";

        [MenuItem(MonoBehaviourMenuPath, false, 1000)]
        static void ToggleMonoBehaviour(MenuCommand command) => Toggle(command.context, MonoBehaviourMenuPath);

        [MenuItem(MonoBehaviourMenuPath, true)]
        static bool ValidateMonoBehaviour(MenuCommand command) => Validate(command.context, MonoBehaviourMenuPath);

        [MenuItem(ScriptableObjectMenuPath, false, 1000)]
        static void ToggleScriptableObject(MenuCommand command) => Toggle(command.context, ScriptableObjectMenuPath);

        [MenuItem(ScriptableObjectMenuPath, true)]
        static bool ValidateScriptableObject(MenuCommand command) => Validate(command.context, ScriptableObjectMenuPath);

        static void Toggle(Object target, string menuPath)
        {
            ViewOnlyManager.Toggle(target.GetInstanceID());
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                window.Repaint();
        }

        static bool Validate(Object target, string menuPath)
        {
            bool hasFields = ViewOnlyManager.HasViewOnlyFields(target.GetType());
            if (hasFields)
                Menu.SetChecked(menuPath, ViewOnlyManager.IsVisible(target.GetInstanceID()));
            return hasFields;
        }
    }
}

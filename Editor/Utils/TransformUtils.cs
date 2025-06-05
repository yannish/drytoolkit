using UnityEditor;
using UnityEngine;

public static class TransformUtils
{
    [InitializeOnLoadMethod]
    public static void Init()
    {
        EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
    }

    private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Vector3)
        {
            menu.AddItem(new GUIContent("Zero out"), false, () =>
            {
                property.vector3Value = Vector3.zero;
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddItem(new GUIContent("Reflect in X"), false, () =>
            {
                property.vector3Value = property.vector3Value.With(x: -property.vector3Value.x);
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddItem(new GUIContent("Reflect in Y"), false, () =>
            {
                property.vector3Value = property.vector3Value.With(y: -property.vector3Value.y);
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddItem(new GUIContent("Reflect in Z"), false, () =>
            {
                property.vector3Value = property.vector3Value.With(z: -property.vector3Value.z);
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        if (property.propertyType == SerializedPropertyType.Quaternion)
        {
            menu.AddItem(new GUIContent("Zero"), false, () =>
            {
                property.quaternionValue = Quaternion.identity;
                property.serializedObject.ApplyModifiedProperties();
            });
            
            menu.AddItem(new GUIContent("Reflect Rotation in X"), false, () =>
            {
                var reflectedRot = ReflectRotation(property.quaternionValue, Vector3.right);
                property.quaternionValue = reflectedRot;
                property.serializedObject.ApplyModifiedProperties();
            });
        }
    }

    static Quaternion ReflectRotation(Quaternion rotation, Vector3 normal)
    {
        var currUp = rotation * Vector3.up;
        var currForward = rotation * Vector3.forward;

        var reflectedUp = Vector3.Reflect(currUp, normal);
        var reflectedForward = Vector3.Reflect(currForward, normal);
                
        var reflectedRot = Quaternion.LookRotation(reflectedForward, reflectedUp);
        
        return reflectedRot;
    }
    
    [MenuItem("CONTEXT/Transform/Mirror Rotation Across X Axis")]
    static void MirrorRotation(MenuCommand command)
    {
        Transform transform = (Transform)command.context;
        
        transform.localPosition = transform.localPosition.With(x : -transform.localPosition.x);
        transform.rotation = ReflectRotation(transform.rotation, Vector3.right);
        
        EditorUtility.SetDirty(transform);
        
        Debug.LogWarning("Mirrored!");
        
        // Vector3 normal = Vector3.right;
        //
        // Matrix4x4 M = Matrix4x4.identity;
        // for (int i = 0; i < 3; i++)
        // {
        //     for (int j = 0; j < 3; j++)
        //     {
        //         M[i, j] -= 2 * normal[i] * normal[j];
        //     }
        // }
        //
        // Matrix4x4 R = Matrix4x4.Rotate(transform.rotation);
        // Matrix4x4 mirrored = M * R * M;
        //
        // Undo.RecordObject(transform, "Mirror Rotation");
        // transform.rotation = mirrored.rotation;
    }
    
}

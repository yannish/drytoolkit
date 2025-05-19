using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace drytoolkit.Editor.NestedAnimation
{
    public class NestedAnimationEditor : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Nested Animation Editor")]
        public static void ShowWindow()
        {
            var openInstances = Resources.FindObjectsOfTypeAll<NestedAnimationEditor>();
            Debug.LogWarning($"open count: {openInstances.Length}");
            foreach (var instance in openInstances)
            {
                instance?.Close();
            }
            
            if (HasOpenInstances<NestedAnimationEditor>())
            {
                FocusWindowIfItsOpen<NestedAnimationEditor>();
                return;
            }
            
            NestedAnimationEditor window = GetWindow<NestedAnimationEditor>();
            window.titleContent = new GUIContent("Nested Animation Editor");
        }

        enum NestedAnimatorEditorState
        {
            VIEW,
            PREVIEW,
            EDIT
        }

        private bool isConnected => connectedAnimatorField.value != null;
        
        private Animator parentAnimator;
        private List<Animator> nestedAnimators;

        private AnimationClip selectedParentClip;
        private AnimationClip selectedNestedClip;

        
        private VisualElement disconnectedElement;
        private VisualElement selectedAnimatorElement;
        private VisualElement connectedAnimatorElement;
        
        private ObjectField selectedAnimatorField;
        private ObjectField connectedAnimatorField;
        
        public void CreateGUI()
        {
            var root = rootVisualElement;
            
            SetColors();
            SetIcons();
            FetchAnimationWindow();

            // selectedAnimatorField = CreateSelectedAnimatorElement();
            disconnectedElement = CreateDisconnectedElement();
            // connectedAnimatorField = CreateConnectedAnimatorField();

            selectedAnimatorElement = CreateObjectFieldWithHeader(
                "Selected Animator",
                out selectedAnimatorField,
                typeof(Animator)
            );

            connectedAnimatorElement = CreateConnectedAnimatorElement(out connectedAnimatorField);
            disconnectedElement = CreateDisconnectedElement();
            
            // connectedAnimatorElement.Add(disconnectedElement);
            
            root.Add(selectedAnimatorElement);
            // root.Add(disconnectedElement);
            root.Add(connectedAnimatorElement);
        }

        private const float headerHeight = 30;
        private VisualElement CreateObjectFieldWithHeader(
            string headerLabel,
            out ObjectField objectField,
            Type fieldType,
            float indent = 0f,
            bool enableObjectField = true,
            bool allowSceneObjects = true
        )
        {
            VisualElement section = new VisualElement();
            section.style.flexDirection = FlexDirection.Row;
            section.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            section.style.height = headerHeight;

            objectField = new ObjectField();
            objectField.objectType = fieldType;
            objectField.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            objectField.style.alignSelf = Align.Center;
            objectField.SetEnabled(enableObjectField);
            
            Label header = new Label(headerLabel);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14; 
            header.style.alignSelf = Align.Center;
            header.style.paddingRight = indent;

            // 🎨 Optional background color
            header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            header.style.color = Color.white; 
            header.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.marginBottom = 4;
            
            section.Add(header);
            section.Add(objectField);

            return section;
        }

        private VisualElement CreateHeaderSection(string headerLabel, float indent = 0f)
        {
            VisualElement headerSection = new VisualElement();
            headerSection.style.flexDirection = FlexDirection.Row;
            headerSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            headerSection.style.height = headerHeight;
            
            Label header = new Label(headerLabel);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14; 
            header.style.alignSelf = Align.Center;
            header.style.paddingRight = indent;

            // 🎨 Optional background color
            header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            header.style.color = Color.white; 
            header.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.marginBottom = 4;
            
            return headerSection;
        }

        private Button connectButton;
        private VisualElement CreateConnectedAnimatorElement(out ObjectField objectField)
        {
            VisualElement headerSection = CreateHeaderSection("Connected Animator");
            headerSection.style.flexDirection = FlexDirection.Column;
            // headerSection.style.flexGrow = 1;
            // headerSection.style.paddingTop = 20;
            
            // VisualElement objectSection = new VisualElement();
            // objectSection.style.flexDirection = FlexDirection.Row;
            
            objectField = new ObjectField();
            objectField.objectType = typeof(Animator);
            objectField.style.flexDirection = FlexDirection.Row;
            // objectField.style.flexGrow = 1;
            // objectField.style.alignSelf = Align.FlexEnd;
            objectField.SetEnabled(false);

            Button connectButton = new Button();
            connectButton.text = "Connect";
            connectButton.SetEnabled(false);
            connectButton.style.alignSelf = Align.FlexEnd;

                    
            headerSection.Add(objectField);
            headerSection.Add(connectButton);
            
            // headerSection.Add(objectSection);
            
            return headerSection;
        }
        
        private ObjectField CreateConnectedAnimatorField()
        {
            connectedAnimatorField = new ObjectField("Connected Animator");
            connectedAnimatorField.objectType = typeof(Animator);
            connectedAnimatorField.style.flexDirection = FlexDirection.Row;
            connectedAnimatorField.allowSceneObjects = true;
            connectedAnimatorField.SetEnabled(false);
            
            connectedAnimatorField.RegisterValueChangedCallback(evt =>
            {
                Debug.LogWarning("Changed connected animator.");
            });
            
            return connectedAnimatorField;
        }

        private ObjectField CreateSelectedAnimatorElement()
        {
            selectedAnimatorField = new ObjectField("Selected Animator");
            selectedAnimatorField.objectType = typeof(Animator);
            selectedAnimatorField.style.flexDirection = FlexDirection.Row;
            // selectedAnimatorField.style.flexGrow = 1;
            // selectedAnimatorField.style.alignSelf = Align.Center;
            selectedAnimatorField.allowSceneObjects = true;
            
            selectedAnimatorField.RegisterValueChangedCallback(evt =>
            {
                Debug.LogWarning("Changed selected animator.");
            });
            
            // currSelectedAnimator.SetEnabled(false);
            return selectedAnimatorField;
        }

        private VisualElement CreateDisconnectedElement()
        {
            var disconnectedMessage = new VisualElement();
            disconnectedMessage.style.flexDirection = FlexDirection.Column;
            disconnectedMessage.style.backgroundColor = backgroundGrey;
            disconnectedMessage.style.borderTopWidth = 1;
            disconnectedMessage.style.borderBottomWidth = 1;
            disconnectedMessage.style.borderLeftWidth = 1;
            disconnectedMessage.style.borderRightWidth = 1;
            disconnectedMessage.style.borderTopColor = Color.gray;
            disconnectedMessage.style.borderBottomColor = Color.gray;
            disconnectedMessage.style.borderLeftColor = Color.gray;
            disconnectedMessage.style.borderRightColor = Color.gray;
            disconnectedMessage.style.paddingTop = 6;
            disconnectedMessage.style.paddingBottom = 6;
            disconnectedMessage.style.paddingLeft = 8;
            disconnectedMessage.style.paddingRight = 8;
            disconnectedMessage.style.marginTop = 8;
            disconnectedMessage.style.marginBottom = 8;
            disconnectedMessage.style.unityFontStyleAndWeight = FontStyle.Italic;
            disconnectedMessage.style.display = DisplayStyle.Flex;

            var messageLabel = new Label("Connect to an Animator.");
            messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            disconnectedMessage.Add(messageLabel);
            
            // disconnectedMessage.Add(selectedAnimatorField);
            
            return disconnectedMessage;
        }


        AnimationWindow animationWindow;
        private void FetchAnimationWindow()
        {
            animationWindow = GetWindow<AnimationWindow>();
            //.. cache some stuff for reflection
        }

        private void SetIcons()
        {
            
        }

        
        private Color backgroundGrey;
        private Color selectedBackgroundColor;
        private Color recordingBackgroundColor;
        private Color previewingBackgroundColor;
        private Color selectedRecordingBackgroundColor;

        private void SetColors()
        {
            backgroundGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
            selectedBackgroundColor = new Color(0.4f, 0.4f, 0.6f, 1f);
            selectedRecordingBackgroundColor = new Color(0.5f, 0.3f, 0.3f, 1f);
            recordingBackgroundColor = new Color(0.295f, 0.145f, 0.145f, 1f);
            previewingBackgroundColor = new Color(0.1568627f, 0.2509804f, 0.2941176f,1f);

        }

        
        public void AddItemsToMenu(GenericMenu menu)
        {
            
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PlasticGui;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace drytoolkit.Editor.NestedAnimation
{
    public class NestedAnimationEditor : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Tools/Nested Animation Editor %#e")]
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
        private Animator connectedAnimator => connectedAnimatorField.value as Animator;
        private Animator selectedAnimator => selectedAnimatorField.value as Animator;
        private Animator nestedAnimator => connectedNestedAnimatorField.value as Animator;
        
        private List<Animator> nestedAnimators;

        private AnimationClip selectedParentClip => selectedClipField.value as AnimationClip;
        
        private AnimationClip selectedNestedClip => selectedNestedClipField.value as AnimationClip;

        
        //... ELEMENTS:
        private VisualElement root;
        private ScrollView scrollView;
        
        private VisualElement disconnectedElement;
        private VisualElement selectedAnimatorHeaderElement;
        private VisualElement connectionButtonsElement;
        private VisualElement connectedAnimatorHeaderElement;
        private VisualElement nestedAnimatorHeaderElement;

        private VisualElement connectedAnimatorHolderElement;
        private ScrollView connectedAnimatorScrollView;

        private VisualElement connectedAnimatorControlsElement;
        private VisualElement nestedAnimatorControlsElement;
        
        private List<VisualElement> animatorSections;
        
        private ObjectField selectedAnimatorField;
        private ObjectField connectedAnimatorField;
        private ObjectField selectedNestedAnimatorField;
        private ObjectField connectedNestedAnimatorField;

        private ObjectField selectedClipField;
        private ObjectField selectedNestedClipField;

        private EnumField currModeField;
        
        private Label connectedAnimatorLabelElement;


        private VisualElement allParentClipElements;
        private VisualElement allNestedClipElements;
        // private List<VisualElement> allClipElements = new List<VisualElement>();
        private List<VisualElement> elementsToDisable = new List<VisualElement>();
        
        
        //... COLORS:
        private Color backgroundGrey;
        private Color selectedBackgroundColor;
        private Color recordingBackgroundColor;
        private Color previewingBackgroundColor;
        private Color selectedRecordingBackgroundColor;
        private Color headerBackgroundColor;

        
        //... TEXTURES:
        private Texture buttonIconTexture;
        private Texture dupeIconTexture;
        private Texture editIconTexture;
        private Texture nestedEditIconTexture;
        private Texture nestedPreviewIconTexture;
        private Texture2D recordIconTexture;
        
        
        //... LABELS:
        private const string selectedAnimatorLabel = "SELECTED:";
        private const string connectedAnimatorLabel = "CONNECTED:";
        private const string controlsLabel = "CONTROLS :";
        private const string parentLabel = "PARENT :";
        private const string nestedLabel = "\u2937 NESTED :";
        private const string enterEditButtonLabel = "EDIT";
        private const string exitEditButtonLabel= "... DONE";
        private const string enterPreviewButtonLabel = "PREVIEW";
        private const string exitPreviewButtonLabel = "... DONE";
        
        
        //... ANIMATION WINDOW REFLECTION CONTROLS:
        private AnimationWindow animWindow;
        private Type animWindowType;
        private Type animWindowStateType;
        private PropertyInfo animWindowStatePropInfo;
        private PropertyInfo recordingPropInfo;
        private PropertyInfo previewingPropInfo;
        private object animWindowState;
        
        
        public void CreateGUI()
        {
            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            rootVisualElement.Add(scrollView);
            
            root = scrollView;

            SetColors();
            SetIcons();
            SetTextures();
            FetchAnimationWindow();

            // selectedAnimatorField = CreateSelectedAnimatorElement();
            // disconnectedElement = CreateDisconnectedElement();
            // connectedAnimatorField = CreateConnectedAnimatorField();
            
            selectedClipField = new ObjectField("Selected Clip");
            selectedClipField.objectType = typeof(AnimationClip);
            // selectedClipField.style.display = DisplayStyle.None;
            root.Add(selectedClipField);
            
            selectedNestedClipField = new ObjectField("Selected Nested Clip");
            selectedNestedClipField.objectType = typeof(AnimationClip);
            // selectedNestedClipField.style.display = DisplayStyle.None;
            root.Add(selectedNestedClipField);


            selectedAnimatorHeaderElement = CreateObjectFieldWithHeader(selectedAnimatorLabel, out selectedAnimatorField, typeof(Animator));
            
            connectionButtonsElement = CreateConnectButtons();
            selectedAnimatorHeaderElement.Add(connectionButtonsElement);
            
            selectedAnimatorField.RegisterValueChangedCallback(evt =>
            {
                connectButton.SetEnabled(evt != null);
            });
            
            connectedAnimatorHeaderElement = CreateConnectedAnimatorElement(out connectedAnimatorField);
            disconnectedElement = CreateDisconnectedElement();
            
            selectedNestedAnimatorField = new ObjectField("Selected Nested Animator");
            selectedNestedAnimatorField.objectType = typeof(Animator);
            
            nestedAnimatorHeaderElement = CreatedNestedAnimatorElement(out connectedNestedAnimatorField);
            nestedAnimatorHeaderElement.style.display = DisplayStyle.None;
            
            // var dummyLabel = new Label("Dummy Label");
            // connectedAnimatorHeaderElement.Add(dummyLabel);
            
            // nestedAnimatorField = new ObjectField("Nested Animator");
            // connectedAnimatorElement.Add(disconnectedElement);
            
            root.Add(selectedAnimatorHeaderElement);
            // root.Add(connectionButtonsElement);
            root.Add(connectedAnimatorHeaderElement);
            root.Add(disconnectedElement);
            root.Add(nestedAnimatorHeaderElement);
            
            Selection.selectionChanged -= HandleSelectionChange;
            Selection.selectionChanged += HandleSelectionChange;
            
            HandleSelectionChange();
            
            // root.Add(connectedAnimatorElement);
        }
        
        private void FetchAnimationWindow()
        {
            //... cache some stuff for reflection
            animWindow = GetWindow<AnimationWindow>();
            animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            animWindowStatePropInfo = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
            animWindowState = animWindowStatePropInfo.GetValue(animWindow);
            animWindowStateType = animWindowState.GetType();
            recordingPropInfo = animWindowStateType.GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
            previewingPropInfo = animWindowStateType.GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
        }

        private void SetIcons()
        {
            
        }

        private void SetColors()
        {
            backgroundGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
            selectedBackgroundColor = new Color(0.4f, 0.4f, 0.6f, 1f);
            selectedRecordingBackgroundColor = new Color(0.5f, 0.3f, 0.3f, 1f);
            recordingBackgroundColor = new Color(0.295f, 0.145f, 0.145f, 1f);
            previewingBackgroundColor = new Color(0.1568627f, 0.2509804f, 0.2941176f,1f);
            headerBackgroundColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        }

        private void SetTextures()
        {
            buttonIconTexture = EditorGUIUtility.IconContent("d_PlayButton").image;
            dupeIconTexture = EditorGUIUtility.IconContent("Animator Icon").image;
            editIconTexture = EditorGUIUtility.IconContent("CollabEdit Icon").image;
            recordIconTexture = EditorGUIUtility.IconContent("Animation.Record").image as Texture2D;
            nestedEditIconTexture = EditorGUIUtility.IconContent("SkinnedMeshRenderer Icon").image;
            nestedPreviewIconTexture = EditorGUIUtility.IconContent("SkinnedMeshRenderer Icon").image;
        }
        

        
        //... CONNECT:
        private Button connectButton;
        private Button disconnectButton;
        private const float buttonMargin = 4;
        private const float buttonMinWidth = 150;
        private const float buttonWidth = 32f;
        private const float connectedAnimatorIndentPadding = 20f;
        private const float connectedAnimatorMarginBottom = 6f;
        
        private VisualElement CreateConnectButtons()
        {
            VisualElement connectButtonSection = new VisualElement();
            connectButtonSection.style.flexDirection = FlexDirection.Row;
            connectButtonSection.style.marginTop = buttonMargin;
            connectButtonSection.style.marginBottom = buttonMargin;
            
            connectButton = new Button();
            connectButton.text = "CONNECT";
            connectButton.SetEnabled(false);
            connectButton.style.minWidth = buttonMinWidth;
            connectButton.style.alignSelf = Align.FlexEnd;
            connectButton.style.flexDirection = FlexDirection.Row;
            // connectButton.style.flex
            connectButton.style.flexGrow = 1;

            connectButton.clicked += () =>
            {
                connectButton.Blur();
                ConnectAnimator();
            };

            
            disconnectButton = new Button();
            disconnectButton.text = "DISCONNECT";
            disconnectButton.SetEnabled(false);
            disconnectButton.style.minWidth = buttonMinWidth;
            disconnectButton.style.alignSelf = Align.FlexEnd;
            disconnectButton.style.flexDirection = FlexDirection.Row;
            disconnectButton.style.flexGrow = 1;

            disconnectButton.clicked += () =>
            {
                connectButton.Blur();
                DisconnectAnimator();
            };
            
            
            connectButtonSection.Add(connectButton);
            connectButtonSection.Add(disconnectButton);
            
            return connectButtonSection;
        }

        
        private VisualElement modeControlElement;
        private Button nestedEditClipButton;
        private Image nestedEditIconImage;
        
        private Button nestedPreviewClipButton;
        private Image nestedPreviewIconImage;
        
        private const float previewButtonWidth = 100f;
        private const float iconSize = 16f;

        
        private void ConnectAnimator()
        {
            DisconnectAnimator();
            
            if (selectedAnimatorField.value == null || animWindow == null)
                return;
            
            if (
                selectedAnimatorField.value is Animator animator
                && animator.runtimeAnimatorController == null
                )
            {
                Debug.LogWarning($"Animator {animator.name} has no controller.");
                return;
            }
            
            connectedAnimatorField.value = selectedAnimatorField.value;
            disconnectedElement.style.display = DisplayStyle.None;
            disconnectButton.SetEnabled(true);
            connectedAnimatorLabelElement.SetEnabled(true);

            //... subscribe to continuously validate animators:
            EditorApplication.update -= EditorApplicationUpdate;
            EditorApplication.update += EditorApplicationUpdate;
            
            
            //... build animator UI:
            connectedAnimatorControlsElement = new VisualElement();
            connectedAnimatorControlsElement.style.paddingTop = 6f;
            connectedAnimatorControlsElement.style.paddingBottom = connectedAnimatorMarginBottom;
            connectedAnimatorControlsElement.style.flexDirection = FlexDirection.Column;
            // connectedAnimatorControlsElement.style.display = DisplayStyle.None;
            // connectedAnimatorControlsElement.style.overflow = Overflow.Hidden;

            
            elementsToDisable.Clear();
            
            allParentClipElements = new VisualElement();
            allParentClipElements.style.flexDirection = FlexDirection.Column;
            
            foreach (var clip in connectedAnimator.runtimeAnimatorController.animationClips)
            {
                var clipElement = new VisualElement();
                clipElement.style.flexDirection = FlexDirection.RowReverse;
                clipElement.style.flexGrow = 1;
                clipElement.style.paddingLeft = connectedAnimatorIndentPadding;
                
                var clipField = new ObjectField();
                clipField.style.flexGrow = 1;
                clipField.objectType = typeof(AnimationClip);
                clipField.value = clip;
                
                var selectIconImage = new Image();
                selectIconImage.style.alignSelf = Align.Center;
                selectIconImage.image = editIconTexture;
                selectIconImage.scaleMode = ScaleMode.ScaleToFit;
                selectIconImage.style.width = 16;
                selectIconImage.style.height = 16;

                var selectClipButton = new Button();
                selectClipButton.style.width = buttonWidth;
                selectClipButton.Add(selectIconImage);
                        
                // selectClipButton.RegisterCallback<FocusInEvent>(e => e.StopPropagation());
                // selectClipButton.RegisterCallback<BlurEvent>(e => e.StopPropagation());

                selectClipButton.clicked += () =>
                {
                    selectClipButton.Blur();
                    selectedClipField.value = clip;
                    
                    if (HasOpenInstances<AnimationWindow>())
                    {
                        if (connectedAnimator.gameObject != Selection.activeGameObject)
                        {
                            Debug.LogWarning("setting selected animator for animWindow's sake!");
                            Selection.activeGameObject = connectedAnimator.gameObject;
                        }
                        
                        var animationWindow = GetWindow<AnimationWindow>();
                        if(animationWindow.animationClip != clip)
                            animationWindow.animationClip = clip;
                        animationWindow.Repaint();
                        
                        UpdateSelectedAnimationClip(allParentClipElements, clip, selectedBackgroundColor);
                    }
                };
                
                clipElement.Add(clipField);
                clipElement.Add(selectClipButton);
                
                //...
                allParentClipElements.Add(clipElement);
                
                //... track so that we can disable in edit mode:
                elementsToDisable.Add(clipElement);
            }
            
            connectedAnimatorControlsElement.Add(allParentClipElements);
            
            
            //... MODAL CONTROLS:
            modeControlElement = new VisualElement();
            modeControlElement.style.flexDirection = FlexDirection.RowReverse;
            modeControlElement.style.paddingTop = 6f;
            
            //... RECORD BUTTON:
            nestedEditIconImage = new Image();
            nestedEditIconImage.style.alignSelf = Align.Center;
            nestedEditIconImage.image = nestedEditIconTexture;
            nestedEditIconImage.scaleMode = ScaleMode.ScaleToFit;
            nestedEditIconImage.style.width = 16;
            nestedEditIconImage.style.height = 16;
            nestedEditIconImage.style.display = DisplayStyle.None;
            
            nestedEditClipButton = new Button();
            nestedEditClipButton.text = enterEditButtonLabel;
            nestedEditClipButton.style.width = previewButtonWidth;
            nestedEditClipButton.style.flexDirection = FlexDirection.RowReverse;

            nestedEditClipButton.clicked += () =>
            {
                nestedEditClipButton.Blur();
                ToggleNestedEdit();
            };

            
            //... PREVIEW BUTTON:
            nestedPreviewIconImage = new Image();
            nestedPreviewIconImage.style.alignSelf = Align.Center;
            nestedPreviewIconImage.image = nestedPreviewIconTexture;
            nestedPreviewIconImage.scaleMode = ScaleMode.ScaleToFit;
            nestedPreviewIconImage.style.width = 16;
            nestedPreviewIconImage.style.height = 16;
            
            nestedPreviewClipButton = new Button();
            nestedPreviewClipButton.text = enterPreviewButtonLabel;
            nestedPreviewClipButton.style.width = previewButtonWidth;
            nestedPreviewClipButton.style.flexDirection = FlexDirection.RowReverse;

            nestedPreviewClipButton.clicked += () =>
            {
                nestedPreviewClipButton.Blur();
                ToggleNestedPreview();
            };
            
            modeControlElement.Add(nestedEditClipButton);
            modeControlElement.Add(nestedPreviewClipButton);
            modeControlElement.Add(nestedEditIconImage);
            
            connectedAnimatorControlsElement.Add(modeControlElement);
            
            
            //... try set selected clip to whatever we stashed in selectedClipField:
            if (
                selectedClipField.value != null 
                && selectedClipField.value is AnimationClip selectedClip
                && connectedAnimator.runtimeAnimatorController.animationClips.Contains(selectedClip)
                )
            {
                // Debug.LogWarning("this animator has the last clip we set as selected");                    
            }
            //... otherwise set the selected clip to whatever's first in the animator controller:
            else
            {
                // Debug.LogWarning("setting to what was in the window");
                selectedClipField.value = animWindow.animationClip;
            }
            
            //... then update our visuals to match:
            UpdateSelectedAnimationClip(allParentClipElements, selectedClipField.value as AnimationClip, selectedBackgroundColor);
            
            //... finally add to the root (which is atm a scrollview)
            //... TODO: maybe scrollview should only cover the clips section...
            connectedAnimatorHeaderElement.Add(connectedAnimatorControlsElement);
            // root.Add(connectedAnimatorControlsElement);
            
            

            // ... NESTED:
            
            if (selectedNestedAnimatorField.value == null)
                return;

            connectedNestedAnimatorField.value = selectedNestedAnimatorField.value;
            
            // if (!(selectedNestedAnimatorField.value is Animator))
            //     return;

            if (nestedAnimator.runtimeAnimatorController == null)
                return;
            
            nestedAnimatorHeaderElement.style.display = DisplayStyle.Flex;

            nestedAnimatorControlsElement = new VisualElement();
            nestedAnimatorControlsElement.style.paddingTop = 6f;
            nestedAnimatorControlsElement.style.paddingBottom = connectedAnimatorMarginBottom;
            nestedAnimatorControlsElement.style.flexDirection = FlexDirection.Column;

            allNestedClipElements = new VisualElement();
            allNestedClipElements.style.flexDirection = FlexDirection.Column;
            
            foreach (var clip in nestedAnimator.runtimeAnimatorController.animationClips)
            {
                var clipElement = new VisualElement();
                clipElement.style.flexDirection = FlexDirection.RowReverse;
                clipElement.style.flexGrow = 1;
                clipElement.style.paddingLeft = connectedAnimatorIndentPadding * 2f;
                
                var clipField = new ObjectField();
                clipField.style.flexGrow = 1;
                clipField.objectType = typeof(AnimationClip);
                clipField.value = clip;

                var spoofButton = new Button();
                spoofButton.style.flexDirection = FlexDirection.Row;
                spoofButton.style.width = buttonWidth;// * 2f;
                spoofButton.style.alignContent = Align.Center;
                spoofButton.style.alignItems = Align.Center;
                
                var recordIconImage = new Image();
                recordIconImage.style.flexGrow = 1;
                recordIconImage.style.flexDirection = FlexDirection.Column;
                recordIconImage.style.alignSelf = Align.Center;
                recordIconImage.image = recordIconTexture;
                recordIconImage.scaleMode = ScaleMode.ScaleToFit;
                recordIconImage.style.width = iconSize;
                recordIconImage.style.height = iconSize;
                recordIconImage.style.flexShrink = 0;
                        
                var spoofIconImage = new Image();
                spoofIconImage.style.alignSelf = Align.Center;
                spoofIconImage.image = dupeIconTexture;
                spoofIconImage.style.width = iconSize;
                spoofIconImage.style.height = iconSize;
                
                //... TODO: set spoof toggle initial value on creation.
                var spoofToggle = new Toggle();
                // if(spoofedClips.Contains(clip))
                //     spoofToggle.value = true;
                // else
                //     spoofToggle.value = false;
                
                spoofToggle.RegisterValueChangedCallback(evt =>
                {
                    // Debug.LogWarning("toggle value changed.");

                    if (evt.newValue)
                    {
                        // if(!spoofedClips.Contains(clip))
                        //     spoofedClips.Add(clip);
                    }
                    else
                    {
                        // if(spoofedClips.Contains(clip))
                        //     spoofedClips.Remove(clip);
                    }
                });
                
                spoofButton.Add(recordIconImage);
                spoofButton.clicked += () =>
                {
                    Debug.LogWarning("Clicked spoof button!");
                    spoofButton.Blur();
                    selectedNestedClipField.value = clip;

                    if (HasOpenInstances<AnimationWindow>())
                    {
                        if (nestedAnimator.gameObject != Selection.activeGameObject)
                        {
                            Debug.LogWarning("setting selected animator for animWindow's sake!");
                            Selection.activeGameObject = nestedAnimator.gameObject;
                        }
                        
                        var animationWindow = GetWindow<AnimationWindow>();
                        if(animationWindow.animationClip != clip)
                            animationWindow.animationClip = clip;
                        animationWindow.Repaint();

                        UpdateSelectedAnimationClip(allNestedClipElements, clip, selectedRecordingBackgroundColor);
                    }
                };
                
                clipElement.Add(clipField);
                clipElement.Add(spoofButton);
                clipElement.Add(spoofIconImage);
                clipElement.Add(spoofToggle);
                
                allNestedClipElements.Add(clipElement);
                
                elementsToDisable.Add(clipElement);
            }
            
            //... try set selected clip to whatever we stashed in selectedClipField:
            if (
                selectedNestedClipField.value != null 
                // && selectedClipField.value is AnimationClip selectedNestedClip
                && nestedAnimator.runtimeAnimatorController.animationClips.Contains(selectedNestedClip)
            )
            {
                // Debug.LogWarning("this animator has the last clip we set as selected");                    
            }
            //... otherwise set the selected clip to whatever's first in the animator controller:
            else
            {
                // Debug.LogWarning("setting to what was in the window");
                selectedNestedClipField.value = nestedAnimator.runtimeAnimatorController.animationClips[0];
            }
            
            UpdateSelectedAnimationClip(allNestedClipElements, selectedNestedClip, selectedRecordingBackgroundColor);
            
            nestedAnimatorControlsElement.Add(allNestedClipElements);
            
            nestedAnimatorHeaderElement.Add(nestedAnimatorControlsElement);
        }

        private void ToggleNestedPreview()
        {
            throw new NotImplementedException();
        }

        private void ToggleNestedEdit()
        {
           
        }

        private void DisconnectAnimator()
        {
            connectedAnimatorField.value = null;
            connectedNestedAnimatorField.value = null;
            
            disconnectedElement.style.display = DisplayStyle.Flex;
            
            disconnectButton?.SetEnabled(false);
            connectedAnimatorLabelElement?.SetEnabled(false);
            
            EditorApplication.update -= EditorApplicationUpdate;

            //... TODO: why Clear some, & not others...
            
            //... shut down UI:
            if (connectedAnimatorControlsElement != null)
            {
                connectedAnimatorControlsElement.Clear();
                connectedAnimatorHeaderElement.Remove(connectedAnimatorControlsElement);
                connectedAnimatorControlsElement = null;
            }
            
            if (nestedAnimatorControlsElement != null)
            {
                nestedAnimatorControlsElement.Clear();
                nestedAnimatorHeaderElement.Remove(nestedAnimatorControlsElement);
                nestedAnimatorControlsElement = null;
            }
                
            nestedAnimatorHeaderElement.style.display = DisplayStyle.None;
        }

        private void UpdateSelectedAnimationClip(VisualElement clipsElement, AnimationClip selectedClip, Color selectedColor)
        {
            foreach (var element in clipsElement.Children())
            {
                element.style.backgroundColor = new Color(0, 0, 0, 0);
                foreach (var subChild in element.Children())
                {
                    if (subChild is ObjectField objectField && objectField.value == selectedClip)
                    {
                        element.style.backgroundColor = selectedColor;
                    }
                }
            }
        }

        private void EditorApplicationUpdate()
        {
            if (isConnected)
            {
                Animator animator = connectedAnimatorField.value as Animator;
                if (animator != null)
                {
                    if (animator.runtimeAnimatorController == null)
                    {
                        Debug.LogWarning("connected animator lost its runtimeAnimatorController, disconnecting.");
                        DisconnectAnimator();
                    }
                }
            }
        }
        

        //... SELECTED ANIMATOR:
        private const float headerPadding = 20;
        private const float headerHeight = 26;
        private const float selectedAnimatorMarginTop = 6;
        private const float selectedAnimatorMarginBottom = 10;
        private const float connectedAnimatorMarginBotton = 6;
        // private const float nestedAnimatorMarginBotton = 2;

        private VisualElement CreateSelectedAnimatorElement(out ObjectField objectField)
        {
            VisualElement columnSection = new VisualElement();
            columnSection.style.flexDirection = FlexDirection.Column;
            columnSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            columnSection.style.marginBottom = selectedAnimatorMarginBottom;
            columnSection.style.marginTop = selectedAnimatorMarginTop;
            
            VisualElement rowSection = new VisualElement();
            rowSection.style.flexDirection = FlexDirection.Row;
            rowSection.style.height = headerHeight;
            // section.style.paddingBottom = headerPadding;

            objectField = new ObjectField();
            objectField.objectType = typeof(Animator);
            objectField.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            objectField.style.alignSelf = Align.Center;
            objectField.SetEnabled(true);
            
            Label header = new Label("SELECTED ANIMATOR");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14; 
            // header.style.alignSelf = Align.Center;
            // header.style.paddingRight = indent;

            // 🎨 Optional background color
            header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            header.style.color = Color.white; 
            header.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.marginBottom = 4;

            rowSection.Add(header);
            rowSection.Add(objectField);
            
            columnSection.Add(rowSection);
            // section.Add(connectButton);

            return columnSection;
        }
        
        private VisualElement CreateObjectFieldWithHeader(
            string headerLabel,
            out ObjectField objectField,
            Type fieldType,
            float indent = 0f,
            bool enableObjectField = true,
            bool allowSceneObjects = true
        )
        {
            VisualElement columnSection = new VisualElement();
            columnSection.style.flexDirection = FlexDirection.Column;
            columnSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            columnSection.style.marginBottom = selectedAnimatorMarginBottom;
            columnSection.style.marginTop = selectedAnimatorMarginTop;
            
            VisualElement rowSection = new VisualElement();
            rowSection.style.flexDirection = FlexDirection.Row;
            rowSection.style.height = headerHeight;
            rowSection.style.backgroundColor = headerBackgroundColor;
            // section.style.paddingBottom = headerPadding;

            objectField = new ObjectField();
            objectField.objectType = fieldType;
            objectField.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            objectField.style.alignSelf = Align.Center;
            // objectField.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); 
            objectField.SetEnabled(enableObjectField);
            
            Label header = new Label(headerLabel);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 14; 
            // header.style.alignSelf = Align.Center;
            header.style.paddingRight = indent;

            // 🎨 Optional background color
            // header.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); 
            header.style.color = Color.white; 
            header.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            header.style.paddingLeft = 6;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.marginBottom = 4;

            rowSection.Add(header);
            rowSection.Add(objectField);
            
            columnSection.Add(rowSection);
            // section.Add(connectButton);

            return columnSection;
        }

        private VisualElement CreateHeaderSection(string headerLabel, float indent = 0f)
        {
            VisualElement headerSection = new VisualElement();
            headerSection.style.flexDirection = FlexDirection.Row;
            headerSection.style.backgroundColor = headerBackgroundColor; 
            headerSection.style.height = headerHeight;
            
            Label label = new Label(headerLabel);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 14; 
            label.style.alignSelf = Align.Center;
            label.style.paddingRight = indent;

            // 🎨 Optional background color
            // label.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            label.style.color = Color.white; 
            label.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            label.style.paddingLeft = 6;
            label.style.paddingTop = 4;
            label.style.paddingBottom = 4;
            label.style.marginBottom = 4;
            
            headerSection.Add(label);
            
            return headerSection;
        }

        
        //... CONNECTED ANIMATOR:
        private VisualElement CreateConnectedAnimatorElement(out ObjectField objectField)
        {
            VisualElement holderSection = new VisualElement();
            holderSection.style.flexDirection = FlexDirection.Column;
            holderSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            holderSection.style.marginBottom = connectedAnimatorMarginBotton;
            
            VisualElement headerSection = CreateHeaderSection(connectedAnimatorLabel);
            // headerSection.style.flexDirection = FlexDirection.Row;
            // headerSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            // headerSection.style.height = headerHeight;
            // headerSection.style.flexGrow = 1;
            // headerSection.style.marginTop = 20;
            
            // VisualElement objectSection = new VisualElement();
            // objectSection.style.flexDirection = FlexDirection.Row;
            
            objectField = new ObjectField();
            objectField.objectType = typeof(Animator);
            objectField.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            objectField.style.alignSelf = Align.Center;
            objectField.SetEnabled(false);

            connectedAnimatorLabelElement = new Label(connectedAnimatorLabel);
            connectedAnimatorLabelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            connectedAnimatorLabelElement.style.fontSize = 14; 
            // header.style.alignSelf = Align.Center;
            // header.style.paddingRight = indent;

            // 🎨 Optional background color
            connectedAnimatorLabelElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); 
            connectedAnimatorLabelElement.style.color = Color.white; 
            connectedAnimatorLabelElement.style.flexGrow = 1;
        
            // 🧱 Padding and spacing
            connectedAnimatorLabelElement.style.paddingLeft = 6;
            connectedAnimatorLabelElement.style.paddingTop = 4;
            connectedAnimatorLabelElement.style.paddingBottom = 4;
            connectedAnimatorLabelElement.style.marginBottom = 4;

            // headerSection.Add(connectedAnimatorLabel);
            headerSection.Add(objectField);
            holderSection.Add(headerSection);

            // headerSection.Add(objectField);
            // headerSection.Add(connectButton);
            
            // headerSection.Add(objectSection);
            
            return holderSection;
        }

        private VisualElement CreatedNestedAnimatorElement(out ObjectField objectField)
        {
            VisualElement holderSection = new VisualElement();
            holderSection.style.flexDirection = FlexDirection.Column;
            holderSection.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            holderSection.style.marginTop = 
            holderSection.style.marginBottom = connectedAnimatorMarginBotton;
            
            VisualElement headerSection = CreateHeaderSection(nestedLabel);
            
            objectField = new ObjectField();
            objectField.objectType = typeof(Animator);
            objectField.style.flexDirection = FlexDirection.Row;
            objectField.style.flexGrow = 1;
            objectField.style.alignSelf = Align.Center;
            objectField.SetEnabled(false);

            headerSection.Add(objectField);
            
            holderSection.Add(headerSection);
            
            // var nestedAnimatorLabel = new Label("Nested Animator");
            
            return holderSection;
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
            disconnectedMessage.style.marginTop = 0;
            disconnectedMessage.style.marginBottom = 8;
            disconnectedMessage.style.unityFontStyleAndWeight = FontStyle.Italic;
            disconnectedMessage.style.display = DisplayStyle.Flex;

            var messageLabel = new Label("Select an Animator and hit connect to start editing.");
            // messageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            disconnectedMessage.Add(messageLabel);
            
            // disconnectedMessage.Add(selectedAnimatorField);
            
            return disconnectedMessage;
        }


        private bool locked = false;
        private void HandleSelectionChange()
        {
            if (locked)
                return;

            
            // if (Selection.activeGameObject == null)
            // {
            //     selectedAnimatorField.value = null;
            //     connectButton.SetEnabled(false);
            //     return;
            // }

            if (Selection.activeGameObject == null)
                return;
            
            var foundAnimators = Selection.activeGameObject.GetComponentsInChildren<Animator>();
            if (foundAnimators.Length == 0)
                return;
            
            //... TODO: set connect button reactivity to the selected animator field value:
            selectedAnimatorField.value = foundAnimators[0];
            // connectButton.SetEnabled(true);

            if (foundAnimators.Length <= 1)
                return;
            
            selectedNestedAnimatorField.value = foundAnimators[1];
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            
        }
        
        
        #region SCRATCH SPACE:


        #endregion

    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class RuntimeRigEditor : EditorWindow, IHasCustomMenu
{
    private Animator parentAnimator;
    private Animator childAnimator;
    
    private ObjectField parentAnimatorField;
    private ObjectField childAnimatorField;

    private AnimationClip sourceClip;
    private AnimationClip targetClip;
    
    private ObjectField sourceClipField;
    private ObjectField targetClipField;

    private ListView clipListView;

    private VisualElement growingClipGrid;
    private VisualElement clipGrid;
    private VisualElement selectionMessage;
    private VisualElement parentSection;
    private VisualElement childSection;
    private VisualElement controlsSection;

    private Texture buttonIconTexture;
    private Texture dupeIconTexture;
    private Texture editIconTexture;
    private Texture recordIconTexture;

    // private bool isRecording = false;
    
    
    private Color backgroundGrey;
    private const float clipFieldWidth = 200f;
    private const float indentPadding = 20f;
    private const float buttonWidth = 24f;
    // private float noteHeight = 
    
    [MenuItem("Tools/Runtime Rig Editor")]
    public static void ShowWindow()
    {
        RuntimeRigEditor wnd = GetWindow<RuntimeRigEditor>();
        wnd.titleContent = new GUIContent("Runtime Rig Editor");
    }

    private void OnDestroy()
    {
        Debug.LogWarning("Destroyed runtime rig editor window.");
        Selection.selectionChanged -= UpdateDisplay;
    }

    public void CreateGUI()
    {
        backgroundGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        buttonIconTexture = EditorGUIUtility.IconContent("d_PlayButton").image;
        dupeIconTexture = EditorGUIUtility.IconContent("Animator Icon").image;
        editIconTexture = EditorGUIUtility.IconContent("CollabEdit Icon").image;
        recordIconTexture = EditorGUIUtility.IconContent("Animation.Record").image;
        
        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;
        
        targetClipField = new ObjectField("Target Clip");
        targetClipField.objectType = typeof(AnimationClip);
        
        sourceClipField = new ObjectField("Source Clip");
        sourceClipField.objectType = typeof(AnimationClip);

        
        //... parent clips are the 'target' into which we might temporarily copy bindings from child clips.
        parentSection = CreateAnimatorSection("PARENT:", out parentAnimatorField, targetClipField, withEditButton: true);
        parentSection.style.display = DisplayStyle.None;
        rootVisualElement.Add(parentSection);
        
        //... child clips are the 'source' which may get copied in temporarily to parent clips.
        childSection = CreateAnimatorSection("CHILD:", out childAnimatorField, sourceClipField, withSpoofButton: true);
        childSection.style.display = DisplayStyle.None;
        rootVisualElement.Add(childSection);
        
        controlsSection = CreateControlsSection(sourceClipField, targetClipField);
        controlsSection.style.display = DisplayStyle.None;
        rootVisualElement.Add(controlsSection);
        
        selectionMessage = CreateSelectionNote();
        rootVisualElement.Add(selectionMessage);
        
        Selection.selectionChanged -= UpdateDisplay;
        Selection.selectionChanged += UpdateDisplay;
        
        UpdateDisplay();
    }


    private VisualElement CreateSelectionNote()
    {
        var messageBox = new VisualElement();
        messageBox.style.flexDirection = FlexDirection.Column;
        messageBox.style.backgroundColor = backgroundGrey;
        messageBox.style.borderTopWidth = 1;
        messageBox.style.borderBottomWidth = 1;
        messageBox.style.borderLeftWidth = 1;
        messageBox.style.borderRightWidth = 1;
        messageBox.style.borderTopColor = Color.gray;
        messageBox.style.borderBottomColor = Color.gray;
        messageBox.style.borderLeftColor = Color.gray;
        messageBox.style.borderRightColor = Color.gray;
        messageBox.style.paddingTop = 6;
        messageBox.style.paddingBottom = 6;
        messageBox.style.paddingLeft = 8;
        messageBox.style.paddingRight = 8;
        messageBox.style.marginTop = 8;
        messageBox.style.marginBottom = 8;
        messageBox.style.unityFontStyleAndWeight = FontStyle.Italic;
        messageBox.style.display = DisplayStyle.Flex; // start hidden

        var messageLabel = new Label("Please select an Animator.");
        messageBox.Add(messageLabel);
        return messageBox;
    }
    
    private VisualElement CreateDummyHeader(string headerLabel, out ObjectField quickObjectField, Type fieldType)
    {
        VisualElement section = new VisualElement();
        section.style.flexDirection = FlexDirection.Row;
        section.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray

        quickObjectField = new ObjectField();
        // quickObjectField.objectType = fieldType;
        
        // ObjectField newObjectFieldA = new ObjectField();
        // newObjectFieldA.objectType = typeof(GameObject);
        //
        // ObjectField newObjectFieldB = new ObjectField();
        // newObjectFieldB.objectType = typeof(GameObject);
        
        // section.Add(newObjectFieldB);
        
        Label header = new Label(headerLabel);

        // // 📏 Make it bold and larger
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14; // Or larger if you like
        header.style.alignSelf = Align.Center;
        //
        // // 🎨 Optional background color
        // header.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Dark gray
        // header.style.color = Color.white; // Text color
        //
        // // 🧱 Padding and spacing
        // header.style.paddingLeft = 6;
        // header.style.paddingTop = 4;
        // header.style.paddingBottom = 4;
        // header.style.marginBottom = 4;
        
        // section.Add(header);
        // section.Add(quickObjectField);
        
        section.Add(header);
        // section.Add(quickObjectField);

        return section;
    }
    
    private VisualElement CreateAnimatorSection(string headerLabel, out ObjectField animatorField, ObjectField clipField, bool withEditButton = false, bool withSpoofButton = false)
    {
        VisualElement section = new VisualElement();
        section.style.paddingTop = 6f;
        section.style.paddingBottom = 20f;
        
        section.style.display = DisplayStyle.None;

        // 🧩 Animator field
        animatorField = new ObjectField();
        animatorField.objectType = typeof(Animator);
        animatorField.label = "";
        animatorField.style.paddingBottom = 6f;
        
        AddObjectFieldHeaderSection(section, headerLabel, animatorField);
        // AddHeaderSection(section, headerLabel);

        // clipField.RegisterValueChangedCallback(evt =>
        // {
        //     
        // });

        section.Add(animatorField);
        
        section.style.flexDirection = FlexDirection.Column;
        section.style.overflow = Overflow.Hidden;

        VisualElement clipList = new VisualElement();
        clipList.style.flexDirection = FlexDirection.Column;

        animatorField.RegisterValueChangedCallback(evt =>
        {
            clipList.Clear();
            
            var animator = evt.newValue as Animator;
            section.style.display = animator != null ? DisplayStyle.Flex : DisplayStyle.None;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    VisualElement entry = new VisualElement();
                    entry.style.flexDirection = FlexDirection.RowReverse;
                    entry.style.flexGrow = 1;
                    entry.style.paddingLeft = indentPadding;
                    
                    var clipListing = new ObjectField();
                    clipListing.style.flexGrow = 1;
                    // clipList.style.width = clipFieldWidth;
                    clipListing.objectType = typeof(AnimationClip);
                    clipListing.value = clip;
                    
                    section.Add(clipListing);

                    entry.Add(clipListing);
                    
                    if (withEditButton)
                    {
                        var iconImage = new Image();
                        iconImage.style.alignSelf = Align.Center;
                        iconImage.image = editIconTexture;
                        iconImage.scaleMode = ScaleMode.ScaleToFit;
                        iconImage.style.width = 16;
                        iconImage.style.height = 16;
                        
                        var selectClipButton = new Button();
                        selectClipButton.style.width = buttonWidth;
                        selectClipButton.Add(iconImage);
                        
                        selectClipButton.RegisterCallback<FocusInEvent>(e =>
                        {
                            e.StopPropagation();  // Prevent focus-related events from propagating
                        });

                        selectClipButton.RegisterCallback<BlurEvent>(e =>
                        {
                            e.StopPropagation();  // Stop the blur events from triggering
                        });
                        
                        selectClipButton.clicked -= null; // Clear previous listeners if reused
                        selectClipButton.clicked += () => selectClipButton.Blur();
                        selectClipButton.clicked += () => OnSelectClipButtonClicked(clip, animator);
                        
                        entry.Add(selectClipButton);
                    }

                    if (withSpoofButton)
                    {
                        var recordIconImage = new Image();
                        recordIconImage.style.alignSelf = Align.Center;
                        recordIconImage.image = recordIconTexture;
                        recordIconImage.scaleMode = ScaleMode.ScaleToFit;
                        recordIconImage.style.width = 16;
                        recordIconImage.style.height = 16;
                        
                        var dupeIconImage = new Image();
                        dupeIconImage.style.alignSelf = Align.Center;
                        dupeIconImage.image = dupeIconTexture;
                        // dupeIconImage.scaleMode = ScaleMode.ScaleToFit;
                        dupeIconImage.style.width = 16;
                        dupeIconImage.style.height = 16;
                        
                        var dupeButton = new Button();
                        dupeButton.style.flexDirection = FlexDirection.Row;
                        dupeButton.style.width = buttonWidth;// * 2f;
                        
                        dupeButton.Add(recordIconImage);
                        entry.Add(dupeIconImage);
                        // dupeButton.Add(dupeIconImage);
                        
                        dupeButton.RegisterCallback<FocusInEvent>(e =>
                        {
                            e.StopPropagation();  // Prevent focus-related events from propagating
                        });

                        dupeButton.RegisterCallback<BlurEvent>(e =>
                        {
                            e.StopPropagation();  // Stop the blur events from triggering
                        });
                        
                        dupeButton.clicked -= null; // Clear previous listeners if reused
                        dupeButton.clicked += () => dupeButton.Blur();
                        dupeButton.clicked += () => OnDupeButtonClicked(clipField, clip, animator);
                        
                        entry.Add(dupeButton);
                    }
                    
                    clipList.Add(entry);
                }
            }
        });
        
        section.Add(clipList);
        
        return section;
    }

    private void OnSelectClipButtonClicked(AnimationClip clip, Animator animator)
    {
        if (clip == null || !HasOpenInstances<AnimationWindow>())
            return;

        if (Selection.activeGameObject == animator.gameObject)
        {
            var animationWindow = GetWindow<AnimationWindow>();
            if (animationWindow.animationClip != clip)
                animationWindow.animationClip = clip;
            animationWindow.Repaint();
        }
    }
    
    private VisualElement CreateControlsSection(ObjectField sourceField, ObjectField targetField)
    {
        VisualElement section = new VisualElement();
        
        // AddHeaderSection(section, "CONTROLS:");
        AddHeaderSection(section, "CONTROLS:");

        Button recordButton = CreateRecordButton();
        Button embedButton = CreateEmbedBindingsButton(sourceField, targetField);
        Button stripButton = CreateStripBindingsButton(sourceField, targetField);
        
        section.Add(targetClipField);
        section.Add(sourceClipField);
        
        section.Add(embedButton);
        section.Add(stripButton);
        section.Add(recordButton);
        
        return section;
    }

    private void AddHeaderSection(VisualElement root, string headerTitle)
    {
        Label header = new Label(headerTitle);

        // 📏 Make it bold and larger
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14; // Or larger if you like

        // 🎨 Optional background color
        header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray
        header.style.color = Color.white; // Text color

        // 🧱 Padding and spacing
        header.style.paddingLeft = 6;
        header.style.paddingTop = 4;
        header.style.paddingBottom = 4;
        header.style.marginBottom = 4;

        // 📐 Stretch to full width of parent
        header.style.flexGrow = 1;
        
        root.Add(header);
    }

    private void AddObjectFieldHeaderSection(VisualElement root, string headerTitle, ObjectField objectField)
    {
        VisualElement section = new VisualElement();
        section.style.flexDirection = FlexDirection.Row;
        section.style.flexWrap = Wrap.NoWrap;
        section.style.flexGrow = 1;
        section.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray
        
        Label header = new Label(headerTitle);

        // 📏 Make it bold and larger
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14; // Or larger if you like

        // 🎨 Optional background color
        header.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Dark gray
        header.style.color = Color.white; // Text color

        // 🧱 Padding and spacing
        header.style.paddingLeft = 6;
        header.style.paddingTop = 4;
        header.style.paddingBottom = 4;
        header.style.marginBottom = 4;

        // 📐 Stretch to full width of parent
        // header.style.flexGrow = 1;
        
        section.Add(header);
        section.Add(objectField);
        
        root.Add(section);
    }

    
    private Button CreateSpoofRecordButton()
    {
        Button spoofButton = new Button();
        spoofButton.text = "SPOOF AND RECORD";
        spoofButton.clicked += ToggleSpoofRecording;
        return spoofButton;
    }

    private Button CreateRecordButton()
    {
        Button recordButton = new Button();
        recordButton.text = "RECORD";
        recordButton.clicked += () =>
        {
            var animationWindow = GetWindow<AnimationWindow>();
            Type animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            if (animWindowType == null)
                return;
            
            // animationWindow.animationClip = targetClip;
                
            var animWindowStateProp = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
            if (animWindowStateProp == null)
                return;
            
            var animWindowState = animWindowStateProp.GetValue(animationWindow);

            var recordProperty = animWindowState.GetType().GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
            if (recordProperty == null)
                return;
            
            var previewingProperty = animWindowState.GetType().GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
            if (previewingProperty == null)
                return;

            var isRecording = (bool) recordProperty.GetValue(animWindowState);
        
            // recordProperty.SetValue(animWindowState, !isRecording);
        
            if(!isRecording)
                recordProperty.SetValue(animWindowState, true);
            else
                previewingProperty.SetValue(animWindowState, false);
        
            Debug.Log($"setting record: {isRecording}");
        };
        
        return recordButton;
    }

    private Button CreateEmbedBindingsButton(ObjectField sourceField, ObjectField targetField)
    {
        Button embedBindingsButton = new Button();
        embedBindingsButton.text = "EMBED SOURCE IN TARGET";
        embedBindingsButton.clicked += () =>
        {
            targetClip = targetField.value as AnimationClip;
            sourceClip = sourceField.value as AnimationClip;

            if (targetClip == null || sourceClip == null)
                return;

            if (childAnimator == null || parentAnimator == null)
                return;
            
            // Debug.LogWarning($"copying clip {sourceClip.name} to {targetClip.name}");

            var rootPath = AnimationUtility.CalculateTransformPath(childAnimator.transform, parentAnimator.transform);
            
            var bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var binding in bindings)
            {
                // Debug.LogWarning($"binding copied: {binding.path}");
                var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                targetClip.SetCurve($"{rootPath}/{binding.path}", binding.type, binding.propertyName, curve);
            }

            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (HasOpenInstances<AnimationWindow>())
            {
                var animationWindow = GetWindow<AnimationWindow>();
            
                animationWindow.animationClip = targetClip;
                
                Debug.Log("updating Animation window.");
                
                Type animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
                
                FieldInfo animEditorField = animWindowType.GetField("m_AnimEditor", BindingFlags.NonPublic | BindingFlags.Instance);
                
                object animEditor = animEditorField?.GetValue(animationWindow);
                if (animEditor == null)
                {
                    Debug.LogWarning("Could not access m_AnimEditor.");
                    return;
                }
                
                // PropertyInfo stateProp = animEditor.GetType().GetProperty("state", BindingFlags.Public | BindingFlags.Instance);
                // object state = stateProp?.GetValue(animEditor);
                // if (state == null)
                // {
                //     Debug.LogWarning("Could not get AnimationWindowState.");
                //     return;
                // }

                // Call: state.Refresh()
                MethodInfo forceRefreshMethod = animWindowType.GetMethod("ForceRefresh", BindingFlags.NonPublic | BindingFlags.Instance);
                if (forceRefreshMethod != null)
                {
                    forceRefreshMethod.Invoke(animationWindow, null);
                    Debug.Log("✅ AnimationWindow ForceRefresh invoked.");
                }
                else
                {
                    Debug.LogWarning("ForceRefresh method not found.");
                }
                
                animationWindow.Repaint();
            }
            
            embedBindingsButton.Blur();
        };
        
        return embedBindingsButton;
    }

    private Button CreateStripBindingsButton(ObjectField sourceField, ObjectField targetField)
    {
        Button stripButton = new Button();
        stripButton.text = "STRIP SOURCE FROM TARGET";
        stripButton.clicked += () =>
        {
            targetClip = targetField.value as AnimationClip;
            sourceClip = sourceField.value as AnimationClip;

            if (targetClip == null || sourceClip == null)
                return;

            if (childAnimator == null || parentAnimator == null)
                return;
            
            var rootPath = AnimationUtility.CalculateTransformPath(childAnimator.transform, parentAnimator.transform);
            var targetClipBindings = AnimationUtility.GetCurveBindings(targetClip);
            
            foreach (var binding in targetClipBindings)
            {
                if (binding.path.StartsWith(rootPath))
                {
                    AnimationUtility.SetEditorCurve(targetClip, binding, null);
                }
            }
            
            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            stripButton.Blur();
        };
        return stripButton;
    }

    
    //... BUTTON METHODS:
    void ToggleSpoofRecording()
    {
        Debug.LogWarning("SPOOF RECORDING");
    }

    
    bool ToggleRecording()
    {
        return false;
    }
    private void OnDupeButtonClicked(ObjectField clipField, AnimationClip clip, Animator animator)
    {
        clipField.value = clip;
        
        // if(animator == childAnimatorField.value)
        // Debug.LogWarning("DUPING CLIP:");
    }

    
    
    //... CALLBACKS:
    private void UpdateDisplay()
    {
        if (locked)
            return;
        
        parentAnimatorField.value = null;
        childAnimatorField.value = null;
        
        childAnimator = null;
        parentAnimator = null;

        if (Selection.activeGameObject == null)
        {
            selectionMessage.style.display = DisplayStyle.Flex;
            parentSection.style.display = DisplayStyle.None;
            childSection.style.display = DisplayStyle.None;
            controlsSection.style.display = DisplayStyle.None;
            return;
        }
        
        var foundAnimators = Selection.activeGameObject.GetComponentsInChildren<Animator>();
        if (foundAnimators.Length == 0)
        {
            parentAnimatorField.value = null;
            childAnimatorField.value = null;
        }

        if (foundAnimators.Length > 0)
        {
            parentAnimatorField.value = foundAnimators[0];
            parentAnimator = foundAnimators[0];
        }

        if (foundAnimators.Length > 1)
        {
            childAnimatorField.value = foundAnimators[1];
            childAnimator = foundAnimators[1];
        }

        if (childAnimator == null && parentAnimator == null)
        {
            selectionMessage.style.display = DisplayStyle.Flex;
            parentSection.style.display = DisplayStyle.None;
            childSection.style.display = DisplayStyle.None;
            controlsSection.style.display = DisplayStyle.None;
        }
        else
        {
            selectionMessage.style.display = DisplayStyle.None;
            
            if(parentAnimator != null)
                parentSection.style.display = DisplayStyle.Flex;
            
            if(childAnimator != null)
                childSection.style.display = DisplayStyle.Flex;
            
            if(parentAnimator != null && childAnimator != null)
                controlsSection.style.display = DisplayStyle.Flex;
        }
    }

    
    private Button CreateCompareButton()
    {
        Button compareButton = new Button();
        compareButton.text = "Compare";
        compareButton.clicked += () =>
        {
            compareButton.Blur();

            if (childAnimator == null || parentAnimator == null)
            {
                Debug.LogWarning("need both a child & parent animator to compare clips.");
                return;
            }
            
            if (targetClipField.value == null || sourceClipField.value == null)
            {
                Debug.LogWarning("need both a target & source clip to compare.");
                return;
            }

            var rootPath = AnimationUtility.CalculateTransformPath(childAnimator.transform, parentAnimator.transform);
            var sourceBindings = AnimationUtility.GetCurveBindings(sourceClipField.value as AnimationClip);
            foreach (var binding in sourceBindings)
            {
                if (binding.path.StartsWith(rootPath))
                {
                    Debug.LogWarning($"... this is a match : {binding.path}");
                }
            }
        };
        return compareButton;
    }

    private Button CreateClearButton()
    {
        Button clearButton = new Button();
        clearButton.text = "Clear";
        clearButton.clicked += () =>
        {
            var targetClip = targetClipField.value as AnimationClip;
            
            if (targetClip != null)
            {
                var curveBindings = AnimationUtility.GetCurveBindings(targetClip);
                foreach (var binding in curveBindings)
                {
                    AnimationUtility.SetEditorCurve(targetClip, binding, null);
                }
                
                EditorUtility.SetDirty(targetClip);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            clearButton.Blur();
        };

        return clearButton;
    }
    

    // Optional: Set min width per object field (to help with responsiveness)
    
    float clipFieldMinWidth = 150;
    
    void CreateGUI_PREV()
    {
        // Object field to select GameObject with Animator
        parentAnimatorField = new ObjectField("Parent Animator");
        parentAnimatorField.objectType = typeof(Animator);
        parentAnimatorField.allowSceneObjects = true;
        parentAnimatorField.SetEnabled(false);
        parentAnimatorField.RegisterValueChangedCallback(OnAnimatorChanged);

        // Container for AnimationClips
        clipGrid = new VisualElement();
        clipGrid.style.flexWrap = Wrap.Wrap;
        clipGrid.style.flexDirection = FlexDirection.Row;
        // clipGrid.style.ga = 4;
        // clipGrid.style.marginTop = 10;

        // Optional: Set min width per object field (to help with responsiveness)
        float clipFieldMinWidth = 150;
        
        childAnimatorField = new ObjectField("Child Animator");
        childAnimatorField.objectType = typeof(Animator);
        childAnimatorField.allowSceneObjects = true;
        childAnimatorField.SetEnabled(false);
        // childAnimatorField.RegisterValueChangedCallback(OnAnimatorChanged);
        
        Selection.selectionChanged += UpdateDisplay;
        
        rootVisualElement.Add(parentAnimatorField);
        rootVisualElement.Add(clipGrid);
        
        rootVisualElement.Add(childAnimatorField);

        // List view to display Animation Clips
        clipListView = new ListView
        {
            style = { flexGrow = 2 },
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.None
        };

        // rootVisualElement.Add(clipListView);
    }
    
    private void OnAnimatorChanged(ChangeEvent<Object> evt)
    {
        Debug.LogWarning("Animator changed.");
        
        var animator = evt.newValue as Animator;
        clipGrid.Clear();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                var clipField = new ObjectField
                {
                    objectType = typeof(AnimationClip),
                    value = clip
                };

                // Make them read-only
                clipField.SetEnabled(false);

                // Style for grid layout
                clipField.style.minWidth = clipFieldMinWidth;
                clipField.style.flexGrow = 1;
                clipField.style.flexBasis = 0;

                clipGrid.Add(clipField);
            }
        }
        
        // GameObject selectedObj = evt.newValue as GameObject;
        // if (selectedObj == null)
        // {
        //     clipListView.itemsSource = null;
        //     clipListView.RefreshItems();
        //     return;
        // }
        //
        // Animator animator = selectedObj.GetComponent<Animator>();
        // if (animator == null || animator.runtimeAnimatorController == null)
        // {
        //     Debug.LogWarning("Selected GameObject has no Animator or no Controller.");
        //     clipListView.itemsSource = null;
        //     clipListView.RefreshItems();
        //     return;
        // }
        //
        // AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        // if (clips.Length == 0)
        // {
        //     Debug.LogWarning("Selected Animator has no Clips.");
        //     clipListView.itemsSource = null;
        //     clipListView.RefreshItems();
        //     return;
        // }
        //
        // List<string> clipNames = new List<string>();
        // foreach (var clip in clips)
        // {
        //     clipNames.Add(clip.name);
        // }
        //
        // clipListView.itemsSource = clips;
        //
        // clipListView.makeItem = () =>
        // {
        //     return new Button();
        // };
        //
        // clipListView.bindItem = (element, i) =>
        // {
        //     var button = element as Button;
        //     AnimationClip clip = clips[i];
        //     button.text = clip.name;
        //
        //     button.clicked -= null; // Clear previous listeners if reused
        //     button.clicked += () => OnClipButtonClicked(clip);
        // };
        //
        // clipListView.RefreshItems();
    }

    private bool locked = false;
    private GUIStyle lockButtonStyle;

    private void ShowButton(Rect rect)
    {
        if(this.lockButtonStyle == null)
            this.lockButtonStyle = "IN LockButton";
        this.locked = GUI.Toggle(rect, this.locked, GUIContent.none, this.lockButtonStyle);
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(
            new GUIContent("Lock"), 
            this.locked,
            () =>
            {
                this.locked = !this.locked;
                UpdateDisplay();
            }
            );
    }
}

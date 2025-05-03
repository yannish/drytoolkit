using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.Profiling.Memory.Experimental;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class RuntimeRigEditor : EditorWindow, IHasCustomMenu
{
    [MenuItem("Tools/Runtime Rig Editor")]
    public static void ShowWindow()
    {
        RuntimeRigEditor wnd = GetWindow<RuntimeRigEditor>();
        wnd.titleContent = new GUIContent("Runtime Rig Editor");
    }

    
    private Animator parentAnimator;
    private Animator nestedAnimator;

    private ObjectField dummyField;
    
    private ObjectField parentAnimatorField;
    private ObjectField nestedAnimatorField;

    private AnimationClip sourceClip;
    private AnimationClip targetClip;
    
    private ObjectField selectedParentClipField;
    private ObjectField selectedNestedClipField;
    
    private ObjectField sourceClipField;
    private ObjectField targetClipField;

    private ListView clipListView;

    private VisualElement growingClipGrid;
    private VisualElement clipGrid;
    private VisualElement selectionMessage;
    private VisualElement parentSection;
    private VisualElement nestedAnimatorSection;
    private VisualElement controlsSection;

    private Texture buttonIconTexture;
    private Texture dupeIconTexture;
    private Texture editIconTexture;
    private Texture2D recordIconTexture;

    
    private Color backgroundGrey;
    private Color selectedBackgroundColor;
    private Color recordingBackgroundColor;
    
    private const float clipFieldWidth = 200f;
    private const float indentPadding = 20f;
    private const float buttonWidth = 32f;
    
    private const float headerHeight = 30f;
    private const float headerPaddingRight = 10f;

    private const float iconSize = 16f;
    private const float buttonSize = 16f;
    
    //... LOCK TOGGLE:
    private bool locked = false;
    private GUIStyle lockButtonStyle;
    
    
    private Dictionary<AnimationClip, VisualElement> parentClipToElementLookup = new Dictionary<AnimationClip, VisualElement>();
    private Dictionary<AnimationClip, VisualElement> nestedClipToElementLookup = new Dictionary<AnimationClip, VisualElement>();


    public void CreateGUI()
    {
        backgroundGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
        selectedBackgroundColor = new Color(0.4f, 0.4f, 0.6f, 1f);
        recordingBackgroundColor = new Color(0.295f, 0.145f, 0.145f, 1f);

        buttonIconTexture = EditorGUIUtility.IconContent("d_PlayButton").image;
        dupeIconTexture = EditorGUIUtility.IconContent("Animator Icon").image;
        editIconTexture = EditorGUIUtility.IconContent("CollabEdit Icon").image;
        recordIconTexture = EditorGUIUtility.IconContent("Animation.Record").image as Texture2D;

        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        // var listView = CreateDummyList();
        // root.Add(listView);
        
        
        //... NOTHING SELECTED:                
        selectionMessage = CreateSelectionNote();
        rootVisualElement.Add(selectionMessage);

        
        //... PARENT:
        parentSection = new VisualElement();
        
        selectedParentClipField = new ObjectField();
        selectedParentClipField.objectType = typeof(AnimationClip);
        root.Add(selectedParentClipField);
        
        var parentHeader = CreateDummyHeader("ANIMATOR:", out parentAnimatorField, typeof(Animator));
        rootVisualElement.Add(parentHeader);
        
        //... parent clips are the 'target' into which we might temporarily copy bindings from child clips.
        var parentControls  = CreateAnimatorSection(
            parentAnimatorField,
            selectedParentClipField,
            parentClipToElementLookup,
            withEditButton: true
            );
        
        parentSection.Add(parentHeader);
        parentSection.Add(parentControls);
        parentSection.style.display = DisplayStyle.None;
        
        rootVisualElement.Add(parentSection);
        
        
        //... NESTED:
        nestedAnimatorSection = new VisualElement();
        
        selectedNestedClipField = new ObjectField();
        selectedNestedClipField.objectType = typeof(AnimationClip);
        root.Add(selectedNestedClipField);
        
        var nestedAnimatorHeader = CreateDummyHeader("\u2937 NESTED:", out nestedAnimatorField, typeof(Animator));
        rootVisualElement.Add(nestedAnimatorHeader);
        
        //... child clips are the 'source' which may get copied in temporarily to parent clips.
        var nestedAnimatorControls = CreateAnimatorSection(
            nestedAnimatorField,
            selectedNestedClipField,
            nestedClipToElementLookup,
            withSpoofButton: true
            );
        
        nestedAnimatorSection.Add(nestedAnimatorHeader);
        nestedAnimatorSection.Add(nestedAnimatorControls);
        nestedAnimatorSection.style.display = DisplayStyle.None;
        
        rootVisualElement.Add(nestedAnimatorSection);
        
        
        //... CONTROLS:
        controlsSection = CreateControlsSection(sourceClipField, targetClipField);
        controlsSection.style.display = DisplayStyle.None;
        rootVisualElement.Add(controlsSection);

        Selection.selectionChanged -= UpdateDisplay;
        Selection.selectionChanged += UpdateDisplay;
        
        UpdateDisplay();
    }
    
    private void OnDestroy()
    {
        Debug.LogWarning("Destroyed runtime rig editor window.");
        Selection.selectionChanged -= UpdateDisplay;
    }


    //... SECTION CREATION:
    private VisualElement CreateDummyList()
    {
        // Sample data
        var items = new string[] { "Item 1", "Item 2", "Item 3", "Item 4" };
    
        // Create ListView
        var listView = new ListView();
    
        listView.itemsSource = items;
        listView.fixedItemHeight = 22;
        listView.selectionType = SelectionType.Single;
    
        // Make item visual element
        listView.makeItem = () =>
        {
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        };
    
        // Bind data to each item
        listView.bindItem = (element, i) =>
        {
            (element as Label).text = items[i];
        };
    
        // listView.style.flexGrow = 1.0f;
        listView.style.marginTop = 4;
        listView.style.marginBottom = 4;
    
        return listView;
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
        messageBox.style.display = DisplayStyle.Flex;

        var messageLabel = new Label("Select an Animator.");
        messageBox.Add(messageLabel);
        return messageBox;
    }
    
    private VisualElement CreateDummyHeader(string headerLabel, out ObjectField objectField, Type fieldType)
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
        
        
        Label header = new Label(headerLabel);

        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14; 
        header.style.alignSelf = Align.Center;
        header.style.paddingRight = headerPaddingRight;

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

    void UpdateClipSelectionDisplay(AnimationClip clip, Dictionary<AnimationClip, VisualElement> clipToElementLookup)
    {
        foreach(var kvp in clipToElementLookup)
            kvp.Value.style.backgroundColor = new Color(0, 0, 0, 0);
                
        if (clipToElementLookup.TryGetValue(clip, out var foundElement))
            foundElement.style.backgroundColor = selectedBackgroundColor;
    }
    
    private VisualElement CreateAnimatorSection(
        ObjectField animatorField, 
        ObjectField clipField, 
        Dictionary<AnimationClip, VisualElement> clipToElementLookup,
        bool withEditButton = false, 
        bool withSpoofButton = false
        )
    {
        VisualElement section = new VisualElement();
        section.style.paddingTop = 6f;
        section.style.paddingBottom = 20f;
        section.style.display = DisplayStyle.None;
        section.style.flexDirection = FlexDirection.Column;
        section.style.overflow = Overflow.Hidden;

        VisualElement clipList = new VisualElement();
        clipList.style.flexDirection = FlexDirection.Column;

        if (withEditButton)
        {
            clipField.RegisterValueChangedCallback(evt =>
            {
                AnimationClip clip = evt.newValue as AnimationClip;
                UpdateClipSelectionDisplay(clip, clipToElementLookup);
            });
        }
        
        animatorField.RegisterValueChangedCallback(evt =>
        {
            clipList.Clear();
            clipToElementLookup.Clear();
            
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
                    clipListing.objectType = typeof(AnimationClip);
                    clipListing.value = clip;
                    
                    // section.Add(clipListing);

                    entry.Add(clipListing);
                    
                    clipToElementLookup.Add(clip, entry);
                    
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
                        selectClipButton.clicked += () => clipField.value = clip;

                        entry.Add(selectClipButton);
                    }

                    if (withSpoofButton)
                    {
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
                        
                        var dupeIconImage = new Image();
                        dupeIconImage.style.alignSelf = Align.Center;
                        dupeIconImage.image = dupeIconTexture;
                        // dupeIconImage.scaleMode = ScaleMode.ScaleToFit;
                        dupeIconImage.style.width = iconSize;
                        dupeIconImage.style.height = iconSize;
                        
                        // dupeButton.style.backgroundImage = new StyleBackground(recordIconTexture);
                        
                        spoofButton.Add(recordIconImage);
                        entry.Add(dupeIconImage);
                        
                        spoofButton.RegisterCallback<FocusInEvent>(e =>
                        {
                            e.StopPropagation();  // Prevent focus-related events from propagating
                        });

                        spoofButton.RegisterCallback<BlurEvent>(e =>
                        {
                            e.StopPropagation();  // Stop the blur events from triggering
                        });
                        
                        spoofButton.clicked -= null; // Clear previous listeners if reused
                        spoofButton.clicked += () => spoofButton.Blur();
                        spoofButton.clicked += () => OnDupeButtonClicked(clipField, clip, animator);
                        
                        entry.Add(spoofButton);
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

        // foreach (var clipElement in allClipVisualElements)
        // {
        //     clipElement.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
        // }
    }
    
    private VisualElement CreateControlsSection(ObjectField sourceField, ObjectField targetField)
    {
        VisualElement section = new VisualElement();
        
        var header = CreateHeaderSection("CONTROLS:");
        section.Add(header);

        Button recordButton = CreateRecordButton();
        Button embedButton = CreateEmbedBindingsButton(sourceField, targetField);
        Button stripButton = CreateStripBindingsButton(sourceField, targetField);
        
        section.Add(embedButton);
        section.Add(stripButton);
        section.Add(recordButton);

        var restoreRemoveSection = CreateNestedAnimatorToggleButton();
        section.Add(restoreRemoveSection);
        
        return section;
    }

    private VisualElement CreateHeaderSection(string headerTitle)
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

        return header;
    }
    


    //... BUTTON CREATION:
    private Button CreateSpoofRecordButton()
    {
        Button spoofButton = new Button();
        spoofButton.text = "SPOOF AND RECORD";
        spoofButton.clicked += ToggleSpoofRecording;
        return spoofButton;
    }

    //... ANIMATION WINDOW REFLECTION CONTROLS:
    private bool isRecording;
    private AnimationWindow animWindow;
    private Type animWindowType;
    private Type animWindowStateType;
    private PropertyInfo animWindowStatePropInfo;
    private object animWindowState;
    private PropertyInfo recordingPropInfo;
    private PropertyInfo previewingPropInfo;
    void CacheAnimWindowInfo()
    {
        animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        animWindowStatePropInfo = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
        recordingPropInfo = animWindowState.GetType().GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
        previewingPropInfo = animWindowState.GetType().GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
    }
    
    private Button CreateRecordButton()
    {
        Button recordButton = new Button();
        recordButton.text = "RECORD";
        
        animWindow= GetWindow<AnimationWindow>();
        animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        animWindowStatePropInfo = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        animWindowStateType = animWindowState.GetType();
        recordingPropInfo = animWindowStateType.GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
        previewingPropInfo = animWindowStateType.GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
        
        recordButton.clicked += () =>
        {
            var animationWindow = GetWindow<AnimationWindow>();
            if (animationWindow == null)
                return;
            
            if (animWindowType == null)
                return;

            if (animWindowStatePropInfo == null)
                return;

            if (recordingPropInfo == null)
                return;

            if (previewingPropInfo == null)
                return;

            var animWindowState = animWindowStatePropInfo.GetValue(animationWindow);
            isRecording = (bool) recordingPropInfo.GetValue(animWindowState);
        
            // recordProperty.SetValue(animWindowState, !isRecording);

            if (!isRecording)
                EnterRecordingMode();      
            else
                ExitRecordingMode();
        
            Debug.Log($"setting record: {isRecording}");
        };
        
        // recordButton.clicked += () =>
        // {
        //     var animationWindow = GetWindow<AnimationWindow>();
        //     Type animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        //     if (animWindowType == null)
        //         return;
        //     
        //     var animWindowStateProp = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
        //     if (animWindowStateProp == null)
        //         return;
        //     
        //     var animWindowState = animWindowStateProp.GetValue(animationWindow);
        //
        //     var recordProperty = animWindowState.GetType().GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
        //     if (recordProperty == null)
        //         return;
        //     
        //     var previewingProperty = animWindowState.GetType().GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
        //     if (previewingProperty == null)
        //         return;
        //
        //     isRecording = (bool) recordProperty.GetValue(animWindowState);
        //
        //     // recordProperty.SetValue(animWindowState, !isRecording);
        //
        //     if(!isRecording)
        //         recordProperty.SetValue(animWindowState, true);
        //     else
        //         previewingProperty.SetValue(animWindowState, false);
        //
        //     EnterRecordingMode();
        //     
        //     Debug.Log($"setting record: {isRecording}");
        // };
        
        return recordButton;
    }

    void EnterRecordingMode()
    {
        Debug.LogWarning("Entering recording mode.");
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        recordingPropInfo.SetValue(animWindowState, true);
        rootVisualElement.style.backgroundColor = recordingBackgroundColor;
        isRecording = true;
    }

    void ExitRecordingMode()
    {
        Debug.LogWarning("Exiting recording mode.");
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        previewingPropInfo.SetValue(animWindowState, false);
        rootVisualElement.style.backgroundColor = new Color(0, 0, 0, 0);
        isRecording = false;
    }

    private VisualElement CreateNestedAnimatorToggleButton()
    {
        VisualElement section = new VisualElement();
        section.style.flexDirection = FlexDirection.Row;
        // section.style.flexGrow = 1;
        
        removeButton = new Button();
        removeButton.text = "REMOVE";
        removeButton.style.display = DisplayStyle.Flex;
        // removeButton.style.vis
        removeButton.style.flexGrow = 1;
        removeButton.clicked += RemoveNestedAnimator;
        
        restoreButton = new Button();
        restoreButton.text = "RESTORE";
        restoreButton.style.display = DisplayStyle.None;
        restoreButton.style.flexGrow = 1;
        restoreButton.clicked += RestoreNestedAnimator;
        
        section.Add(removeButton);
        section.Add(restoreButton);
        
        return section;
    }

    Button removeButton;
    Button restoreButton;
    
    private bool cachingNestedAnimator;
    private RuntimeAnimatorController cachedNestedAnimator;
    private GameObject cachedNestedAnimatorGameObject;
    void RemoveNestedAnimator()
    {
        if (nestedAnimator == null)
        {
            Debug.LogWarning("Tried to remove nested animator, but it was null.");
            return;
        }

        cachedNestedAnimatorGameObject = nestedAnimator.gameObject;
        cachedNestedAnimator = nestedAnimator.runtimeAnimatorController;
        DestroyImmediate(nestedAnimator);

        cachingNestedAnimator = true;
        removeButton.style.display = DisplayStyle.None;
        restoreButton.style.display = DisplayStyle.Flex;
    }

    void RestoreNestedAnimator()
    {
        if (!cachingNestedAnimator)
            return;
        
        cachingNestedAnimator = false;
        removeButton.style.display = DisplayStyle.Flex;
        restoreButton.style.display = DisplayStyle.None;

        nestedAnimator = cachedNestedAnimatorGameObject.AddComponent<Animator>();
        nestedAnimator.runtimeAnimatorController = cachedNestedAnimator;
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

            if (nestedAnimator == null || parentAnimator == null)
                return;
            
            // Debug.LogWarning($"copying clip {sourceClip.name} to {targetClip.name}");

            var rootPath = AnimationUtility.CalculateTransformPath(nestedAnimator.transform, parentAnimator.transform);
            
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

            if (nestedAnimator == null || parentAnimator == null)
                return;
            
            var rootPath = AnimationUtility.CalculateTransformPath(nestedAnimator.transform, parentAnimator.transform);
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
    
    private Button CreateCompareButton()
    {
        Button compareButton = new Button();
        compareButton.text = "Compare";
        compareButton.clicked += () =>
        {
            compareButton.Blur();

            if (nestedAnimator == null || parentAnimator == null)
            {
                Debug.LogWarning("need both a child & parent animator to compare clips.");
                return;
            }
            
            if (targetClipField.value == null || sourceClipField.value == null)
            {
                Debug.LogWarning("need both a target & source clip to compare.");
                return;
            }

            var rootPath = AnimationUtility.CalculateTransformPath(nestedAnimator.transform, parentAnimator.transform);
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

    
    //... BUTTON METHODS:
    private void ToggleSpoofRecording()
    {
        Debug.LogWarning("SPOOF RECORDING");
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

        if (parentAnimator != null && Selection.activeGameObject != null)
        {
            var foundAnimatorsInParent = Selection.activeGameObject.GetComponentsInParent<Animator>();
            if (foundAnimatorsInParent != null && foundAnimatorsInParent.Contains(parentAnimator))
            {
                Debug.LogWarning("Still in parent hierarchy.");
                return;
            }
        }
        
        parentAnimatorField.value = null;
        nestedAnimatorField.value = null;
        
        nestedAnimator = null;
        parentAnimator = null;

        ExitNestedAnimationContext();
        
        if (Selection.activeGameObject == null)
        {
            selectionMessage.style.display = DisplayStyle.Flex;
            parentSection.style.display = DisplayStyle.None;
            nestedAnimatorSection.style.display = DisplayStyle.None;
            controlsSection.style.display = DisplayStyle.None;
            return;
        }
        
        var foundAnimators = Selection.activeGameObject.GetComponentsInChildren<Animator>();
        if (foundAnimators.Length == 0)
        {
            parentAnimatorField.value = null;
            nestedAnimatorField.value = null;
        }

        if (foundAnimators.Length > 0)
        {
            parentAnimatorField.value = foundAnimators[0];
            parentAnimator = foundAnimators[0];

            if (HasOpenInstances<AnimationWindow>())
            {
                Debug.LogWarning("reflecting clip selected in animation window.");
                var animationWindow = GetWindow<AnimationWindow>();
                
                selectedParentClipField.value = animationWindow.animationClip;
                UpdateClipSelectionDisplay(animationWindow.animationClip, parentClipToElementLookup);

                animationWindow.Repaint();
            }
        }

        if (foundAnimators.Length > 1)
        {
            nestedAnimatorField.value = foundAnimators[1];
            nestedAnimator = foundAnimators[1];
        }

        if (nestedAnimator == null && parentAnimator == null)
        {
            selectionMessage.style.display = DisplayStyle.Flex;
            parentSection.style.display = DisplayStyle.None;
            nestedAnimatorSection.style.display = DisplayStyle.None;
            controlsSection.style.display = DisplayStyle.None;
        }
        else
        {
            selectionMessage.style.display = DisplayStyle.None;
            
            if(parentAnimator != null)
                parentSection.style.display = DisplayStyle.Flex;
            
            if(nestedAnimator != null)
                nestedAnimatorSection.style.display = DisplayStyle.Flex;
            
            if(parentAnimator != null && nestedAnimator != null)
                controlsSection.style.display = DisplayStyle.Flex;
        }
    }

    private void ExitNestedAnimationContext()
    {
        ExitRecordingMode();
        RestoreNestedAnimator();
    }


    // Optional: Set min width per object field (to help with responsiveness)
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
    
    
     // private void OnAnimatorChanged(ChangeEvent<Object> evt)
    // {
    //     Debug.LogWarning("Animator changed.");
    //     
    //     var animator = evt.newValue as Animator;
    //     clipGrid.Clear();
    //
    //     if (animator != null && animator.runtimeAnimatorController != null)
    //     {
    //         var clips = animator.runtimeAnimatorController.animationClips;
    //         foreach (var clip in clips)
    //         {
    //             var clipField = new ObjectField
    //             {
    //                 objectType = typeof(AnimationClip),
    //                 value = clip
    //             };
    //
    //             // Make them read-only
    //             clipField.SetEnabled(false);
    //
    //             // Style for grid layout
    //             clipField.style.minWidth = clipFieldMinWidth;
    //             clipField.style.flexGrow = 1;
    //             clipField.style.flexBasis = 0;
    //
    //             clipGrid.Add(clipField);
    //         }
    //     }
    //     
    //     // GameObject selectedObj = evt.newValue as GameObject;
    //     // if (selectedObj == null)
    //     // {
    //     //     clipListView.itemsSource = null;
    //     //     clipListView.RefreshItems();
    //     //     return;
    //     // }
    //     //
    //     // Animator animator = selectedObj.GetComponent<Animator>();
    //     // if (animator == null || animator.runtimeAnimatorController == null)
    //     // {
    //     //     Debug.LogWarning("Selected GameObject has no Animator or no Controller.");
    //     //     clipListView.itemsSource = null;
    //     //     clipListView.RefreshItems();
    //     //     return;
    //     // }
    //     //
    //     // AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
    //     // if (clips.Length == 0)
    //     // {
    //     //     Debug.LogWarning("Selected Animator has no Clips.");
    //     //     clipListView.itemsSource = null;
    //     //     clipListView.RefreshItems();
    //     //     return;
    //     // }
    //     //
    //     // List<string> clipNames = new List<string>();
    //     // foreach (var clip in clips)
    //     // {
    //     //     clipNames.Add(clip.name);
    //     // }
    //     //
    //     // clipListView.itemsSource = clips;
    //     //
    //     // clipListView.makeItem = () =>
    //     // {
    //     //     return new Button();
    //     // };
    //     //
    //     // clipListView.bindItem = (element, i) =>
    //     // {
    //     //     var button = element as Button;
    //     //     AnimationClip clip = clips[i];
    //     //     button.text = clip.name;
    //     //
    //     //     button.clicked -= null; // Clear previous listeners if reused
    //     //     button.clicked += () => OnClipButtonClicked(clip);
    //     // };
    //     //
    //     // clipListView.RefreshItems();
    // }
    
    
    // float clipFieldMinWidth = 150;
    //
    // void CreateGUI_PREV()
    // {
    //     // Object field to select GameObject with Animator
    //     parentAnimatorField = new ObjectField("Parent Animator");
    //     parentAnimatorField.objectType = typeof(Animator);
    //     parentAnimatorField.allowSceneObjects = true;
    //     parentAnimatorField.SetEnabled(false);
    //     parentAnimatorField.RegisterValueChangedCallback(OnAnimatorChanged);
    //
    //     // Container for AnimationClips
    //     clipGrid = new VisualElement();
    //     clipGrid.style.flexWrap = Wrap.Wrap;
    //     clipGrid.style.flexDirection = FlexDirection.Row;
    //     // clipGrid.style.ga = 4;
    //     // clipGrid.style.marginTop = 10;
    //
    //     // Optional: Set min width per object field (to help with responsiveness)
    //     float clipFieldMinWidth = 150;
    //     
    //     childAnimatorField = new ObjectField("Child Animator");
    //     childAnimatorField.objectType = typeof(Animator);
    //     childAnimatorField.allowSceneObjects = true;
    //     childAnimatorField.SetEnabled(false);
    //     // childAnimatorField.RegisterValueChangedCallback(OnAnimatorChanged);
    //     
    //     Selection.selectionChanged += UpdateDisplay;
    //     
    //     rootVisualElement.Add(parentAnimatorField);
    //     rootVisualElement.Add(clipGrid);
    //     
    //     rootVisualElement.Add(childAnimatorField);
    //
    //     // List view to display Animation Clips
    //     clipListView = new ListView
    //     {
    //         style = { flexGrow = 2 },
    //         virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
    //         selectionType = SelectionType.None
    //     };
    //
    //     // rootVisualElement.Add(clipListView);
    // }
    
    
    // private void AddObjectFieldHeaderSection(VisualElement root, string headerTitle, ObjectField objectField)
    // {
    //     VisualElement section = new VisualElement();
    //     section.style.flexDirection = FlexDirection.Row;
    //     section.style.flexWrap = Wrap.NoWrap;
    //     section.style.flexGrow = 1;
    //     section.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark gray
    //     
    //     Label header = new Label(headerTitle);
    //
    //     // 📏 Make it bold and larger
    //     header.style.unityFontStyleAndWeight = FontStyle.Bold;
    //     header.style.fontSize = 14; // Or larger if you like
    //
    //     // 🎨 Optional background color
    //     header.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Dark gray
    //     header.style.color = Color.white; // Text color
    //
    //     // 🧱 Padding and spacing
    //     header.style.paddingLeft = 6;
    //     header.style.paddingTop = 4;
    //     header.style.paddingBottom = 4;
    //     header.style.marginBottom = 4;
    //
    //     // 📐 Stretch to full width of parent
    //     // header.style.flexGrow = 1;
    //     
    //     section.Add(header);
    //     section.Add(objectField);
    //     
    //     root.Add(section);
    // }
}

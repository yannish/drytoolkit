using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


[InitializeOnLoad]
public static class RuntimeRigEditorUtils
{
    static RuntimeRigEditorUtils()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        CompilationPipeline.compilationStarted += OnBeforeCompilationStarted;
    }

    private static void OnBeforeCompilationStarted(object obj) => CloseDownPreviewing();
    
    private static void OnBeforeAssemblyReload() => CloseDownPreviewing();

    private static void CloseDownPreviewing()
    {
        Debug.LogWarning("Handling recompile for RuntimeRigEditor");
        
        if (!EditorWindow.HasOpenInstances<RuntimeRigEditor>())  
            return;
            
        var foundEditors = Resources.FindObjectsOfTypeAll<RuntimeRigEditor>();
        
        Debug.LogWarning($"found {foundEditors.Length} RuntimeRigEditors...");
        
        runtimeRigEditor = EditorWindow.GetWindow<RuntimeRigEditor>();
        if (runtimeRigEditor != null)
        {
            Debug.LogWarning("Close down previewing of RuntimeRigEditor before compile.");
            
            runtimeRigEditor.ClearGUI();
            // runtimeRigEditor.Close();
            // runtimeRigEditor.CreateGUI();
            AssemblyReloadEvents.afterAssemblyReload -= RebuildGUI;
            AssemblyReloadEvents.afterAssemblyReload += RebuildGUI;
        }
    }
    
    private static RuntimeRigEditor runtimeRigEditor;
    private static void RebuildGUI()
    {
        runtimeRigEditor.CreateGUI();
        runtimeRigEditor = null;
        AssemblyReloadEvents.afterAssemblyReload -= RebuildGUI;
    }
}

public class RuntimeRigEditor : EditorWindow, IHasCustomMenu
{
    [MenuItem("Tools/Runtime Rig Editor %#e")]
    public static void ShowWindow()
    {
        Debug.LogWarning("hit the item!");
        if (HasOpenInstances<RuntimeRigEditor>())
        {
            FocusWindowIfItsOpen<RuntimeRigEditor>();
            return;
        }
        
        RuntimeRigEditor wnd = GetWindow<RuntimeRigEditor>();
        wnd.titleContent = new GUIContent("Runtime Rig Editor");
    }

    
    private Animator parentAnimator;
    private Animator nestedAnimator;

    private AnimationClip sourceClip;
    private AnimationClip targetClip;

    private Dictionary<AnimationClip, VisualElement> parentClipToElementLookup = new Dictionary<AnimationClip, VisualElement>();
    private Dictionary<AnimationClip, VisualElement> nestedClipToElementLookup = new Dictionary<AnimationClip, VisualElement>();

    private List<AnimationClip> spoofedClips = new List<AnimationClip>();
    

    #region ELEMENTS:
    private Toggle logDebugToggle;
    
    private ObjectField parentAnimatorField;
    private ObjectField nestedAnimatorField;

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

    private VisualElement modeControlSection;
    private Button nestedEditClipButton;
    private Image nestedEditIconImage;
    
    
    private Button nestedPreviewClipButton;
    private Image nestedPreviewIconImage;
    
    private Button removeButton;
    private Button restoreButton;
    
    private List<VisualElement> elementsToDisable = new List<VisualElement>();
    #endregion

    #region LAYOUT CONSTANTS :
    private const string controlsLabel = "CONTROLS :";
    private const string parentLabel = "PARENT :";
    private const string nestedLabel = "\u2937 NESTED :";
    private const string enterEditButtonLabel = "EDIT";
    private const string exitEditButtonLabel= "... DONE";
    private const string enterPreviewButtonLabel = "PREVIEW";
    private const string exitPreviewButtonLabel = "... DONE";

    private const float clipFieldWidth = 200f;
    private const float indentPadding = 20f;
    private const float buttonWidth = 32f;
    private const float previewButtonWidth = 100f;
    
    private const float headerHeight = 30f;
    private const float headerPaddingRight = 10f;

    private const float iconSize = 16f;
    private const float buttonSize = 16f;
    #endregion
    
    private Texture buttonIconTexture;
    private Texture dupeIconTexture;
    private Texture editIconTexture;
    private Texture nestedEditIconTexture;
    private Texture nestedPreviewIconTexture;
    private Texture2D recordIconTexture;

    private Color backgroundGrey;
    private Color selectedBackgroundColor;
    private Color recordingBackgroundColor;
    private Color previewingBackgroundColor;
    private Color selectedRecordingBackgroundColor;
    

    //... CACHING NESTED ANIMATOR:
    private bool cachingNestedAnimator;
    private RuntimeAnimatorController cachedNestedAnimator;
    private GameObject cachedNestedAnimatorGameObject;
    private AnimationClip cachedNestedAnimatorClip;
    
    
    //... LOCK TOGGLE:
    private bool locked = false;
    private GUIStyle lockButtonStyle;
    private GUIStyle debugButtonStyle;


    //... ANIMATION WINDOW REFLECTION CONTROLS:
    private AnimationWindow animWindow;
    private Type animWindowType;
    private Type animWindowStateType;
    private PropertyInfo animWindowStatePropInfo;
    private object animWindowState;
    private PropertyInfo recordingPropInfo;
    private PropertyInfo previewingPropInfo;
    
    
    //... BIT OF STATE:
    private bool isRecording;
    private bool isInNestedEdit;
    private bool isInNestedPreview;
    private bool logDebug;
    

    public void ClearGUI()
    {
        var root = rootVisualElement;
        root.Clear();
    }
    
    public void CreateGUI()
    {
        backgroundGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
        selectedBackgroundColor = new Color(0.4f, 0.4f, 0.6f, 1f);
        selectedRecordingBackgroundColor = new Color(0.5f, 0.3f, 0.3f, 1f);
        recordingBackgroundColor = new Color(0.295f, 0.145f, 0.145f, 1f);
        previewingBackgroundColor = new Color(0.1568627f, 0.2509804f, 0.2941176f,1f);

        buttonIconTexture = EditorGUIUtility.IconContent("d_PlayButton").image;
        dupeIconTexture = EditorGUIUtility.IconContent("Animator Icon").image;
        editIconTexture = EditorGUIUtility.IconContent("CollabEdit Icon").image;
        recordIconTexture = EditorGUIUtility.IconContent("Animation.Record").image as Texture2D;
        nestedEditIconTexture = EditorGUIUtility.IconContent("SkinnedMeshRenderer Icon").image;
        nestedPreviewIconTexture = EditorGUIUtility.IconContent("SkinnedMeshRenderer Icon").image;

        //... ANIM WINDOW CONNECTION:
        animWindow = GetWindow<AnimationWindow>();
        animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
        animWindowStatePropInfo = animWindowType.GetProperty("state", BindingFlags.NonPublic | BindingFlags.Instance);
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        animWindowStateType = animWindowState.GetType();
        recordingPropInfo = animWindowStateType.GetProperty("recording", BindingFlags.Public | BindingFlags.Instance);
        previewingPropInfo = animWindowStateType.GetProperty("previewing", BindingFlags.Public | BindingFlags.Instance);
        
        
        var root = rootVisualElement;
        root.Clear(); // Helps prevent duplicates
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        // var listView = CreateDummyList();
        // root.Add(listView);
        
        
        //... NOTHING SELECTED:                
        selectionMessage = CreateSelectionNote();
        rootVisualElement.Add(selectionMessage);
        
        
        //.. HELPER LISTS:
        elementsToDisable = new List<VisualElement>();
        spoofedClips = new List<AnimationClip>();
        
        
        //... PARENT:
        parentSection = new VisualElement();
        
        selectedParentClipField = new ObjectField();
        selectedParentClipField.objectType = typeof(AnimationClip);
        selectedParentClipField.style.display = DisplayStyle.None;
        root.Add(selectedParentClipField);
        
        var parentHeader = CreateHeader(parentLabel, out parentAnimatorField, typeof(Animator), headerPaddingRight);
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
        selectedNestedClipField.style.display = DisplayStyle.None;
        root.Add(selectedNestedClipField);
        
        var nestedAnimatorHeader = CreateHeader(nestedLabel, out nestedAnimatorField, typeof(Animator), headerPaddingRight);
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
        // // controlsSection.Add(selectedNestedClipField);
        // // controlsSection.Add(selectedNestedClipField);
        // rootVisualElement.Add(controlsSection);

        Selection.selectionChanged -= UpdateDisplay;
        Selection.selectionChanged += UpdateDisplay;

        EditorApplication.playModeStateChanged -= HandlePlaymodeChanged;
        EditorApplication.playModeStateChanged += HandlePlaymodeChanged;
        
        UpdateDisplay();
    }

    private void HandlePlaymodeChanged(PlayModeStateChange obj)
    {
        
    }

    private void OnDestroy()
    {
        Debug.LogWarning("Destroyed runtime rig editor window.");
        Selection.selectionChanged -= UpdateDisplay;
    }


    
    //... SECTION CREATION:
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
    
    private VisualElement CreateHeader(string headerLabel, out ObjectField objectField, Type fieldType, float indent = 0f, bool enableObjectField = false)
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
                UpdateParentClipSelectionDisplay(clip, clipToElementLookup);
            });
        }

        if (withSpoofButton)
        {
            clipField.RegisterValueChangedCallback(evt =>
            {
                // Debug.LogWarning("Clip field callback!");
                AnimationClip clip = evt.newValue as AnimationClip;
                UpdateNestedClipSelectionDisplay(clip, clipToElementLookup);
            });
        }
        
        animatorField.RegisterValueChangedCallback(evt =>
        {
            clipList.Clear();
            clipToElementLookup.Clear();

            // if (withSpoofButton)
            // {
            //     spoofedClips.Clear();
            // }
            
            var animator = evt.newValue as Animator;
            section.style.display = animator != null ? DisplayStyle.Flex : DisplayStyle.None;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    VisualElement entry = new VisualElement();
                    entry.style.flexDirection = FlexDirection.RowReverse;
                    entry.style.flexGrow = 1;
                    entry.style.paddingLeft = indentPadding * (withSpoofButton ? 2f : 1f);
                    
                    var clipListing = new ObjectField();
                    clipListing.style.flexGrow = 1;
                    clipListing.objectType = typeof(AnimationClip);
                    clipListing.value = clip;
                    
                    // section.Add(clipListing);

                    entry.Add(clipListing);
                    
                    clipToElementLookup.Add(clip, entry);
                    
                    if (withEditButton)
                    {
                        var selectIconImage = new Image();
                        selectIconImage.style.alignSelf = Align.Center;
                        selectIconImage.image = editIconTexture;
                        selectIconImage.scaleMode = ScaleMode.ScaleToFit;
                        selectIconImage.style.width = 16;
                        selectIconImage.style.height = 16;
                        
                        var selectClipButton = new Button();
                        selectClipButton.style.width = buttonWidth;
                        selectClipButton.Add(selectIconImage);
                        
                        selectClipButton.RegisterCallback<FocusInEvent>(e => e.StopPropagation());
                        selectClipButton.RegisterCallback<BlurEvent>(e => e.StopPropagation());
                        
                        selectClipButton.clicked += () => selectClipButton.Blur();
                        selectClipButton.clicked += () => OnSelectClipButtonClicked(clip, animator);
                        selectClipButton.clicked += () => clipField.value = clip;

                        entry.Add(selectClipButton);
                        elementsToDisable.Add(selectClipButton);
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
                        
                        var spoofIconImage = new Image();
                        spoofIconImage.style.alignSelf = Align.Center;
                        spoofIconImage.image = dupeIconTexture;
                        spoofIconImage.style.width = iconSize;
                        spoofIconImage.style.height = iconSize;
                        
                        // Debug.LogWarning("creating new toggle");
                        
                        var spoofToggle = new Toggle();
                        if(spoofedClips.Contains(clip))
                            spoofToggle.value = true;
                        else
                            spoofToggle.value = false;
                        
                        spoofToggle.RegisterValueChangedCallback(evt =>
                        {
                            // Debug.LogWarning("toggle value changed.");

                            if (evt.newValue)
                            {
                                if(!spoofedClips.Contains(clip))
                                    spoofedClips.Add(clip);
                            }
                            else
                            {
                                if(spoofedClips.Contains(clip))
                                    spoofedClips.Remove(clip);
                            }
                        });
                        
                        // spoofToggle
                        // dupeButton.style.backgroundImage = new StyleBackground(recordIconTexture);
                        
                        spoofButton.Add(recordIconImage);
                        
                        spoofButton.RegisterCallback<FocusInEvent>(e => e.StopPropagation());
                        spoofButton.RegisterCallback<BlurEvent>(e => e.StopPropagation());
                        
                        spoofButton.clicked += () => spoofButton.Blur();
                        // spoofButton.clicked += () => OnDupeButtonClicked(clipField, clip, animator);
                        spoofButton.clicked += () => clipField.value = clip;
                        
                        entry.Add(spoofButton);
                        entry.Add(spoofIconImage);
                        entry.Add(spoofToggle);
                        
                        elementsToDisable.Add(spoofButton);
                    }
                    
                    clipList.Add(entry);
                }
            }
        });
        
        section.Add(clipList);

        if (withEditButton)
        {
            modeControlSection = new VisualElement();
            modeControlSection.style.flexDirection = FlexDirection.RowReverse;
            modeControlSection.style.display = DisplayStyle.None; //... only shown with both parent & nested animator
            
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
            
            // nestedEditClipButton.Add(nestedEditIconImage);
            // nestedEditClipButton.style.display = DisplayStyle.None; //... only shown with both parent & nested animator
                            
            nestedEditClipButton.RegisterCallback<FocusInEvent>(e => e.StopPropagation());
            nestedEditClipButton.RegisterCallback<BlurEvent>(e => e.StopPropagation());
     
            nestedEditClipButton.clicked += () => nestedEditClipButton.Blur();
            nestedEditClipButton.clicked += ToggleNestedEdit;
            
            modeControlSection.Add(nestedEditClipButton);
            
            
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
            
            // nestedPreviewClipButton.Add(nestedPreviewIconImage);
            // nestedPreviewClipButton.style.display = DisplayStyle.None; //... only shown with both parent & nested animator

            nestedPreviewClipButton.RegisterCallback<FocusInEvent>(e => e.StopPropagation());
            nestedPreviewClipButton.RegisterCallback<BlurEvent>(e => e.StopPropagation());
            
            nestedPreviewClipButton.clicked += () => nestedPreviewClipButton.Blur();
            nestedPreviewClipButton.clicked += ToggleNestedPreview;
            
            modeControlSection.Add(nestedPreviewClipButton);
            
            modeControlSection.Add(nestedEditIconImage);
            
            section.Add(modeControlSection);

        }
        
        return section;
    }


    private VisualElement CreateControlsSection(ObjectField sourceField, ObjectField targetField)
    {
        VisualElement section = new VisualElement();
        
        var header = CreateHeaderSection(controlsLabel);
        section.Add(header);

        Button recordButton = CreateRecordButton();
        Button embedButton = CreateEmbedBindingsButton(sourceField, targetField);
        Button stripButton = CreateStripBindingsButton();
        //
        section.Add(embedButton);
        section.Add(stripButton);
        section.Add(recordButton);
        //
        // var restoreRemoveSection = CreateNestedAnimatorToggleButton();
        // section.Add(restoreRemoveSection);

        logDebugToggle = new Toggle("logDebug");
        logDebugToggle.RegisterValueChangedCallback(evt =>
        {
            logDebug = evt.newValue;
        });
        
        section.Add(logDebugToggle);
        
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
    
    
    private void UpdateParentClipSelectionDisplay(AnimationClip clip, Dictionary<AnimationClip, VisualElement> clipToElementLookup)
    {
        foreach(var kvp in clipToElementLookup)
            kvp.Value.style.backgroundColor = new Color(0, 0, 0, 0);
                
        if (clipToElementLookup.TryGetValue(clip, out var foundElement))
            foundElement.style.backgroundColor = selectedBackgroundColor;
    }

    private void UpdateNestedClipSelectionDisplay(AnimationClip clip, Dictionary<AnimationClip, VisualElement> clipToElementLookup)
    {
        if(clip == null)
            return;
        
        foreach(var kvp in clipToElementLookup)
            kvp.Value.style.backgroundColor = new Color(0, 0, 0, 0);

        if (clipToElementLookup.TryGetValue(clip, out var foundElement))
            foundElement.style.backgroundColor = selectedRecordingBackgroundColor;
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

    
    private void ToggleNestedEdit()
    {
        isInNestedEdit = !isInNestedEdit;

        if (isInNestedEdit)
        {
            if(logDebug)
                Debug.LogWarning("Preview with clip-spoofing.");
            
            EmbedNestedAnimationClipBindings();
            CacheNestedAnimator();
            EnterNestedEditMode();
            
            nestedEditClipButton.text = exitEditButtonLabel;
            nestedEditIconImage.image = recordIconTexture;
            nestedEditIconImage.style.display = DisplayStyle.Flex;
        }
        else
        {
            if(logDebug)
                Debug.LogWarning("... exiting preview with clip-spoofing.");

            RestoreNestedAnimationClipBindings();
            RestoreNestedAnimator();
            ExitNestedEditMode();
            
            nestedEditClipButton.text = enterEditButtonLabel;
            // nestedEditIconImage.image = nestedEditIconTexture;
            nestedEditIconImage.style.display = DisplayStyle.None;
        }
    }
    
    private void EnterNestedEditMode()
    {
        if(logDebug)
            Debug.LogWarning("Entering recording mode.");
        
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        recordingPropInfo.SetValue(animWindowState, true);
        rootVisualElement.style.backgroundColor = recordingBackgroundColor;
        
        nestedPreviewClipButton.SetEnabled(false);
        foreach(var element in elementsToDisable)
            element.SetEnabled(false);
    }

    private void ExitNestedEditMode()
    {
        if(logDebug)
            Debug.LogWarning("Exiting recording mode.");
        
        StripNestedAnimationClip();
        
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        previewingPropInfo.SetValue(animWindowState, false);
        rootVisualElement.style.backgroundColor = new Color(0, 0, 0, 0);
        
        nestedPreviewClipButton.SetEnabled(true);
        foreach(var element in elementsToDisable)
            element.SetEnabled(true);
    }

    
    private void ToggleNestedPreview()
    {
        isInNestedPreview = !isInNestedPreview;

        if (isInNestedPreview)
        {
            EmbedNestedAnimationClipBindings();
            CacheNestedAnimator();
            EnterPreviewMode();
            
            nestedPreviewClipButton.text = exitPreviewButtonLabel;
            nestedEditIconImage.image = nestedPreviewIconTexture;
            nestedEditIconImage.style.display = DisplayStyle.Flex;
            // nestedPreviewIconImage.style.display = DisplayStyle.None;
        }
        else
        {
            RestoreNestedAnimationClipBindings();
            RestoreNestedAnimator();
            ExitPreviewMode();
            
            nestedPreviewClipButton.text = enterPreviewButtonLabel;
            nestedEditIconImage.style.display = DisplayStyle.None;
            // nestedPreviewIconImage.style.display = DisplayStyle.Flex;
            // nestedEditIconImage.image = recordIconTexture;
        }
    }
    
    private void EnterPreviewMode()
    {
        if(logDebug)
            Debug.LogWarning("Entering recording mode.");
        
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        previewingPropInfo.SetValue(animWindowState, true);
        rootVisualElement.style.backgroundColor = previewingBackgroundColor;
        
        nestedEditClipButton.SetEnabled(false);
        foreach(var element in elementsToDisable)
            element.SetEnabled(false);
        //
        // foreach(var clip in spoofedClips)
        //     Debug.LogWarning($"{clip.name} will be spoofed.");
    }

    private void ExitPreviewMode()
    {
        if(logDebug)
            Debug.LogWarning("Exiting recording mode.");
        
        StripNestedAnimationClip();
        
        animWindowState = animWindowStatePropInfo.GetValue(animWindow);
        previewingPropInfo.SetValue(animWindowState, false);
        rootVisualElement.style.backgroundColor = new Color(0, 0, 0, 0);
        
        nestedEditClipButton.SetEnabled(true);
        foreach(var element in elementsToDisable)
            element.SetEnabled(true);
    }
    
    private void ExitPreviewMode_OLD()
    {
        if (!isInNestedEdit)
            return;
        
        StripNestedAnimationClip();
        RestoreNestedAnimator();
    }


    //... BUTTON CREATION:
    private Button CreateRecordButton()
    {
        Button recordButton = new Button();
        recordButton.text = "RECORD";
        
        animWindow = GetWindow<AnimationWindow>();
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
            isInNestedEdit = (bool) recordingPropInfo.GetValue(animWindowState);
        
            if (!isInNestedEdit)
                EnterNestedEditMode();      
            else
                ExitNestedEditMode();
        
            Debug.Log($"setting record: {isInNestedEdit}");
        };
        
        return recordButton;
    }



    private VisualElement CreateNestedAnimatorToggleButton()
    {
        VisualElement section = new VisualElement();
        section.style.flexDirection = FlexDirection.Row;
        
        removeButton = new Button();
        removeButton.text = "REMOVE";
        removeButton.style.display = DisplayStyle.Flex;
        removeButton.style.flexGrow = 1;
        removeButton.clicked += CacheNestedAnimator;
        
        restoreButton = new Button();
        restoreButton.text = "RESTORE";
        restoreButton.style.display = DisplayStyle.None;
        restoreButton.style.flexGrow = 1;
        restoreButton.clicked += RestoreNestedAnimator;
        
        section.Add(removeButton);
        section.Add(restoreButton);
        
        return section;
    }

    private Button CreateEmbedBindingsButton(ObjectField sourceField, ObjectField targetField)
    {
        Button embedBindingsButton = new Button();
        embedBindingsButton.text = "EMBED SOURCE IN TARGET";
        
        embedBindingsButton.clicked += () =>
        {
            EmbedNestedAnimationClipBindings();
            embedBindingsButton.Blur();
        };
        
        return embedBindingsButton;
    }

    private Button CreateStripBindingsButton()
    {
        Button stripButton = new Button();
        stripButton.text = "STRIP SOURCE FROM TARGET";
        stripButton.clicked += () =>
        {
            StripNestedAnimationClip();
            stripButton.Blur();
        };
        
        return stripButton;
    }
    
    
    
    //... BUTTON METHODS:
    private void CacheNestedAnimator()
    {
        if (nestedAnimator == null)
        {
            Debug.LogWarning("Tried to remove nested animator, but it was null.");
            return;
        }

        cachedNestedAnimatorGameObject = nestedAnimator.gameObject;
        cachedNestedAnimator = nestedAnimator.runtimeAnimatorController;
        nestedAnimatorField.style.display = DisplayStyle.None;
        
        cachedNestedAnimatorClip = selectedNestedClipField.value as AnimationClip;
        selectedNestedClipField.value = null;
        
        DestroyImmediate(nestedAnimator);

        // removeButton.style.display = DisplayStyle.None;
        // restoreButton.style.display = DisplayStyle.Flex;
        
        cachingNestedAnimator = true;
    }

    private void RestoreNestedAnimator()
    {
        if (!cachingNestedAnimator)
            return;
        
        // removeButton.style.display = DisplayStyle.Flex;
        // restoreButton.style.display = DisplayStyle.None;

        nestedAnimator = cachedNestedAnimatorGameObject.AddComponent<Animator>();
        nestedAnimator.runtimeAnimatorController = cachedNestedAnimator;
        nestedAnimatorField.value = nestedAnimator;

        selectedNestedClipField.value = cachedNestedAnimatorClip;
        
        // var foundClips = nestedAnimator.runtimeAnimatorController.animationClips;
        // if (foundClips.Length > 0)
        // {
        //     var firstClip = foundClips[0];
        //     selectedNestedClipField.value = firstClip;
        //     // UpdateNestedClipSelectionDisplay(firstClip, nestedClipToElementLookup);   
        // }
        
        // UpdateNestedClipSelectionDisplay(selectedNestedClipField.value as AnimationClip, nestedClipToElementLookup);
        
        nestedAnimatorField.style.display = DisplayStyle.Flex;
        
        cachingNestedAnimator = false;
    }
    
    private void StripNestedAnimationClip() 
    {
        if(logDebug)
            Debug.LogWarning("... stripping nested animation clip");

        targetClip = selectedParentClipField.value as AnimationClip;
        sourceClip = selectedNestedClipField.value as AnimationClip;

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
    }

    private void RestoreNestedAnimationClipBindings()
    {
        if(logDebug)
            Debug.LogWarning("Unembedding nested animation clip with edits");
        
        //... now parent clip is the source, nested clip is target:
        sourceClip = selectedParentClipField.value as AnimationClip;
        targetClip = selectedNestedClipField.value as AnimationClip;

        if (targetClip == null || sourceClip == null)
            return;

        if (cachedNestedAnimatorGameObject == null || parentAnimator == null)
            return;
        
        //... CLEAR EVERYTHING FROM NESTED CLIP:
        var nestedClipBindings = AnimationUtility.GetCurveBindings(targetClip);
        foreach (var binding in nestedClipBindings)
        {
            AnimationUtility.SetEditorCurve(targetClip, binding, null);
        }
        
        //... RE-WRITE BINDINGS WITH THE NEST-ROOT BACK TO NESTED CLIP:
        var rootPath = AnimationUtility.CalculateTransformPath(cachedNestedAnimatorGameObject.transform, parentAnimator.transform);
        var sourceClipBindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        foreach (var binding in sourceClipBindings)
        {
            if (binding.path.StartsWith(rootPath))
            {
                AnimationUtility.SetEditorCurve(targetClip, binding, null);
                
                var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                var nestedPath = binding.path.Remove(0, rootPath.Length + 1);
                targetClip.SetCurve(nestedPath, binding.type, binding.propertyName, curve);
            }
        }
        
        //... CLEAR NESTED BINDINGS FROM PARENT CLIP:
        foreach (var binding in sourceClipBindings)
        {
            if (binding.path.StartsWith(rootPath))
            {
                AnimationUtility.SetEditorCurve(targetClip, binding, null);
            }
        }
            
        EditorUtility.SetDirty(targetClip);
        EditorUtility.SetDirty(sourceClip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private void EmbedNestedAnimationClipBindings()
    {
        if(logDebug)
            Debug.LogWarning("Embedding nested animation clip");
        
        targetClip = selectedParentClipField.value as AnimationClip;
        sourceClip = selectedNestedClipField.value as AnimationClip;

        if (targetClip == null || sourceClip == null)
            return;

        if (nestedAnimator == null || parentAnimator == null)
            return;
        
        var rootPath = AnimationUtility.CalculateTransformPath(nestedAnimator.transform, parentAnimator.transform);
        var bindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        foreach (var binding in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            targetClip.SetCurve($"{rootPath}/{binding.path}", binding.type, binding.propertyName, curve);
        }

        /*
         * include "spoof" clips. these will be copied into parent clip, but won't be copied back out afterwards.
         */
        foreach (var clip in spoofedClips)
        {
            if (clip == null)
            {
                Debug.LogWarning("a spoof clip was null!");
                continue;
            }
            
            var spoofBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in spoofBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                targetClip.SetCurve($"{rootPath}/{binding.path}", binding.type, binding.propertyName, curve);
            }
        }

        EditorUtility.SetDirty(targetClip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (HasOpenInstances<AnimationWindow>())
        {
            var animationWindow = GetWindow<AnimationWindow>();
        
            animationWindow.animationClip = targetClip;
            
            Type animWindowType = Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            
            FieldInfo animEditorField = animWindowType.GetField("m_AnimEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            
            object animEditor = animEditorField?.GetValue(animationWindow);
            if (animEditor == null)
            {
                Debug.LogWarning("Could not access m_AnimEditor.");
                return;
            }

            MethodInfo forceRefreshMethod = animWindowType.GetMethod("ForceRefresh", BindingFlags.NonPublic | BindingFlags.Instance);
            if (forceRefreshMethod != null)
            {
                forceRefreshMethod.Invoke(animationWindow, null);
                // Debug.Log("✅ AnimationWindow ForceRefresh invoked.");
            }
            
            animationWindow.Repaint();
        }
    }

    private void OnDupeButtonClicked(ObjectField clipField, AnimationClip clip, Animator animator)
    {
        clipField.value = clip;
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
                if(logDebug)
                    Debug.LogWarning("Still in parent hierarchy.");
                return;
            }
        }
        
        //... only show the preview nested clip button if we've got both animators in place:
        modeControlSection.style.display = DisplayStyle.None;
        // nestedEditClipButton.style.display = DisplayStyle.None;
        
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
            // controlsSection.style.display = DisplayStyle.None;
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
                if(logDebug)
                    Debug.LogWarning("reflecting clip selected in animation window.");

                var animationWindow = GetWindow<AnimationWindow>();
                if (animationWindow != null && animationWindow.animationClip != null)
                {
                    selectedParentClipField.value = animationWindow.animationClip;
                    UpdateParentClipSelectionDisplay(animationWindow.animationClip, parentClipToElementLookup);
                    animationWindow.Repaint();
                }
            }
        }

        if (foundAnimators.Length > 1)
        {
            nestedAnimatorField.value = foundAnimators[1];
            nestedAnimator = foundAnimators[1];

            var foundClips = nestedAnimator.runtimeAnimatorController.animationClips;
            if (foundClips.Length > 0)
            {
                var firstClip = foundClips[0];
                selectedNestedClipField.value = firstClip;
                UpdateNestedClipSelectionDisplay(firstClip, nestedClipToElementLookup);   
            }
            
            modeControlSection.style.display = DisplayStyle.Flex;
        }

        if (nestedAnimator == null && parentAnimator == null)
        {
            selectionMessage.style.display = DisplayStyle.Flex;
            parentSection.style.display = DisplayStyle.None;
            nestedAnimatorSection.style.display = DisplayStyle.None;
            // controlsSection.style.display = DisplayStyle.None;
        }
        else
        {
            selectionMessage.style.display = DisplayStyle.None;
            
            if(parentAnimator != null)
                parentSection.style.display = DisplayStyle.Flex;
            
            if(nestedAnimator != null)
                nestedAnimatorSection.style.display = DisplayStyle.Flex;
            
            // if(parentAnimator != null && nestedAnimator != null)
            //     controlsSection.style.display = DisplayStyle.Flex;
        }
    }

    private void ExitNestedAnimationContext()
    {
        ExitNestedEditMode();
        RestoreNestedAnimator();
    }


    private void ShowButton(Rect rect)
    {
        if(lockButtonStyle == null)
            lockButtonStyle = "IN LockButton";

        // if (debugButtonStyle == null)
        //     debugButtonStyle = "MiniButton";

        using (new GUILayout.HorizontalScope())
        {
            locked = GUI.Toggle(rect, locked, GUIContent.none, lockButtonStyle);
            // logDebug = GUI.Toggle(rect, logDebug, GUIContent.none, debugButtonStyle);
        }
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
        
        menu.AddItem(new GUIContent("Debug"), this.logDebug, () => logDebug = !logDebug);
    }

    
    #region SKETCHPAD:
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
    #endregion
}

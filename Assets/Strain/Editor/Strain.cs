//-------------------------------
//--- Strain - Stylized Hair Tool
//--- Version 1.3
//--- © The Famous Mouse™
//-------------------------------

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering;

public class Strain : EditorWindow
{
    //Window Icons
    Texture toolImage, buttonTexture, buttonTexture2, buttonTexture3;
    GUIContent buttonTexture_con, buttonTexture_con2, buttonTexture_con3;
    
    //Window Properties
    bool selectedObject, editObject;
    int tab = 1;
    GameObject objectToEdit;
    
    //Editor Properties
    GameObject prefabStrain = null, prefabPhysicsStrain = null;
    
    Vector3 brushPos, brushAngle;
    List<GameObject> strains = new List<GameObject>();
    string strainLayer = "Strains";
    bool initializedSetup = false, layerExists = false,
    skinnedMesh = false, simulateStrains = false;
    MeshCollider tempMesh = null;
    GameObject boneReference = null;
    Mesh bakedMesh = null;
    Shader URP, HDRP;
    Material strainMaterial = null;
    
    //Add Tool Properties
    int strainSegments = 30, windForce = 30;
    float strainSegmentsDistance = 0.1f, strainSize = 5f, distanceApart = 0.5f,
    strainWeight = 3f, strainStiffness = 5000f;
    Color strainColor = Color.black;
    Texture2D strainTexture = null;
    Vector2 strainTexTiling = new Vector2(1,1);
    bool windAnimation = false, physics = false, useWindForce = false;
    AnimationCurve strainAnimation = new AnimationCurve(new Keyframe(0, 0.25f), new Keyframe(1, 0f)),
    strainShape = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.3f));
    Vector3 windDirection = new Vector3(35, 0, 0);
    
    //Remove Tool Properties
    float removeRadius = 10f;
    
    //Styling Tool Properties
    float brushSize = 30f, brushStrength = 1f, floatBrushSegments = 0.0005f, weightStrain = 6f;
    int IntBrushSegments = 1;
    Color colorStrain = Color.black;
    bool styleInBrushDirection = true, segmentDistance = false;
    Vector3 styleInDirection;
    enum Actions { PushStrain, PullStrain, GrowStrain, ShrinkStrain, WeightStrain, ColorStrain }
    Actions action = Actions.PullStrain;
    
    /////////////
    //Show window
    [MenuItem("TFM/Strain - Stylized Hair Tool", false, 1)]
    private static void Initialize()
    {
        Strain window  = (Strain)EditorWindow.GetWindow(typeof(Strain), false, "Strain v1.3");
    }
    
    
    ///////////
    //On Enable
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        
        //Tool Image
        this.toolImage = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Strain_logo.png", typeof(Texture));
        
        //This button
        this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon.png", typeof(Texture));
        this.buttonTexture_con = new GUIContent(buttonTexture);
                    
        //Other Buttons
        this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2_0.png", typeof(Texture));
        this.buttonTexture_con2 = new GUIContent(buttonTexture2);
                    
        this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3_0.png", typeof(Texture));
        this.buttonTexture_con3 = new GUIContent(buttonTexture3);
        
        //Prefab Initialization
        if(prefabStrain == null)
        {
            prefabStrain = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Strain/Resources/Strains/NormalStrain/Strain.prefab", typeof(GameObject));
        }
        
        if(prefabPhysicsStrain == null)
        {
            prefabPhysicsStrain = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Strain/Resources/Strains/PhysicsStrain/PhysicsStrain.prefab", typeof(GameObject));
        }
        
        //Set simulate
        simulateStrainsInEditor = true;
        
        //Clear strain list if already filled
        strains.Clear();
        
        if(GraphicsSettings.renderPipelineAsset != null)
        {
            strainMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/Strain/Resources/Materials/StrainMat.mat", typeof(Material));
            
            URP = Shader.Find("Universal Render Pipeline/Lit");
            HDRP = Shader.Find("HDRP/Lit");
            
            if(GraphicsSettings.currentRenderPipeline)
            {
               if(GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
               {
                  strainMaterial.shader = HDRP;
               }
               else
               {
                  strainMaterial.shader = URP;
               }
            }
        }
    }
    
    ////////////
    //On Disable
    private void OnDisable()
    {
        ResetTool();
        
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    /////////////////////////////
    //On Object Selection Changed
    private void OnSelectionChange()
    {
        ResetTool();
        
        //This button
        this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon.png", typeof(Texture));
        this.buttonTexture_con = new GUIContent(buttonTexture);
                    
        //Other Buttons
        this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2_0.png", typeof(Texture));
        this.buttonTexture_con2 = new GUIContent(buttonTexture2);
                    
        this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3_0.png", typeof(Texture));
        this.buttonTexture_con3 = new GUIContent(buttonTexture3);
    }
    
    ////////
    //On GUI
    private void OnGUI()
    {
        //Display Tool Image
        EditorGUILayout.BeginHorizontal();
        EditorGUI.DrawRect(new Rect(0, 0, position.width, 125), Color.yellow);
        GUILayout.Label(toolImage, new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter}, GUILayout.Height(120));
        EditorGUILayout.EndHorizontal();

        GUI.skin.label.wordWrap = true;

        //Prompt project setup update (Physics strain purposes)
        if(Time.fixedDeltaTime != 0.002f)
        {
            if(initializedSetup)
            {
                initializedSetup = false;
            }
            
            EditorGUILayout.BeginVertical("box");
            GUI.contentColor = Color.yellow;
            EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
            EditorGUILayout.LabelField("Project Settings", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
            EditorGUILayout.LabelField("Requires Updating For Strain", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
            EditorGUILayout.LabelField("To Work Best", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
            EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical("box");
            GUI.contentColor = Color.white;
            if(GUILayout.Button("Setup Project"))
            { 
                Time.fixedDeltaTime = 0.002f;
                initializedSetup = true;
                Debug.Log("Strain - Project Settings Updated");
            }
            EditorGUILayout.EndVertical();
        }
        
        else if(Time.fixedDeltaTime == 0.002f && !initializedSetup)
        {
            initializedSetup = true;
        }
        
        if(initializedSetup)
        {
            //Select & assign mesh renderer object
            if(!selectedObject && Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<MeshRenderer>())
            {
                objectToEdit = Selection.activeGameObject;
                selectedObject = true;
            }
            
            //Select & assign skinned mesh renderer object
            else if(!selectedObject && Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>())
            {
                objectToEdit = Selection.activeGameObject;
                skinnedMesh = true;
                selectedObject = true;
            }
            
            //Prompt selection
            else if(!selectedObject && !Selection.activeGameObject)
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("No Object Selected", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("Select A Mesh GameObject", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.EndVertical();
            }
            
            //Prompt mesh object selection
            else if(!selectedObject && Selection.activeGameObject != null && !Selection.activeGameObject.GetComponent<MeshFilter>())
            {
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.yellow;
                EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("Select A Mesh GameObject", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.EndVertical();
            }
            
            //Reset when no selection
            else if(selectedObject && Selection.activeGameObject == null)
            {
                
                ResetTool();

                //This button
                this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon.png", typeof(Texture));
                this.buttonTexture_con = new GUIContent(buttonTexture);

                //Other Buttons
                this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2_0.png", typeof(Texture));
                this.buttonTexture_con2 = new GUIContent(buttonTexture2);

                this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3_0.png", typeof(Texture));
                this.buttonTexture_con3 = new GUIContent(buttonTexture3);
            }


            //Initialize & Edit Selected Object
            if(selectedObject && objectToEdit != null && !editObject)
            {
                if(skinnedMesh)
                {
                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.yellow;
                    EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("A Skinned Mesh Has Been Detected", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("Assign The Prefered Bone Transform", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("For Strains To Be Attached To", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.white;
                    boneReference = (GameObject)EditorGUILayout.ObjectField("Bone Reference", boneReference, typeof(GameObject), true);
                    EditorGUILayout.EndVertical();
                    
                    //Edit object
                    if(boneReference != null)
                    {
                        //Note
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.grey;
                        EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("Object's collider will be used", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("for raycast drawing.", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("A temp mesh collider will be", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("created if none found.", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.white;
                        if(GUILayout.Button("Edit Object"))
                        {   
                            if(!editObject)
                            {
                                editObject = true;
                                Tools.hidden = true;
                                
                                
                                //Add mesh collider if none
                                if(!Selection.activeGameObject.GetComponent<Collider>())
                                {
                                    tempMesh = Selection.activeGameObject.AddComponent<MeshCollider>();
                                }
                                
                                //Skinned mesh collider bake
                                if(Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>())
                                {
                                    bakedMesh = new Mesh();
                                    MeshCollider meshCollider = Selection.activeGameObject.GetComponent<MeshCollider>(); 
                                    SkinnedMeshRenderer skinnedRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();

                                    skinnedRenderer.BakeMesh(bakedMesh, true);
                                    meshCollider.sharedMesh = bakedMesh;
                                }
                            }

                            else
                            {
                                editObject = false;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }

                else
                {
                    //Note
                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.grey;
                    EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("Object's collider will be used", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("for raycast drawing.", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("A temp mesh collider will be", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("created if none found.", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.LabelField("•-----------------------------------•", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.white;
                    if(GUILayout.Button("Edit Object"))
                    {
                        if(!editObject)
                        {
                            editObject = true;
                            Tools.hidden = true;
                            
                            //Add mesh collider if none
                            if(!Selection.activeGameObject.GetComponent<Collider>())
                            {
                                tempMesh = Selection.activeGameObject.AddComponent<MeshCollider>();
                            }
                            
                            //Assigning skinned mesh to mesh collider
                            if(Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>())
                            {
                                Selection.activeGameObject.GetComponent<MeshCollider>().sharedMesh = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                            }
                        }

                        else
                        {
                            editObject = false;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

            }
            
            
            //Tab Properties
            //--------------

            //Tab Navigation Buttons icons etc.
            if(selectedObject && objectToEdit != null && editObject)
            {
                GUILayout.Space(5);
                
                //Simulate in editor
                EditorGUILayout.BeginVertical("box");
                GUI.contentColor = Color.white;
                simulateStrainsInEditor = EditorGUILayout.Toggle("  Simulate In Editor", simulateStrainsInEditor);
                EditorGUILayout.EndVertical();
                
                //Button texts
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField("Add", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("Remove", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.LabelField("Style", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.EndHorizontal();

                //Image Buttons
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button(buttonTexture_con, GUILayout.Width((position.width / 3) - 4f), GUILayout.Height(50)))
                {
                    if(tab != 1)
                    {
                        tab = 1;

                        //This button
                        this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon.png", typeof(Texture));
                        this.buttonTexture_con = new GUIContent(buttonTexture);

                        //Other Buttons
                        this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2_0.png", typeof(Texture));
                        this.buttonTexture_con2 = new GUIContent(buttonTexture2);

                        this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3_0.png", typeof(Texture));
                        this.buttonTexture_con3 = new GUIContent(buttonTexture3);
                    }
                }

                if(GUILayout.Button(buttonTexture_con2, GUILayout.Width((position.width / 3) - 4f), GUILayout.Height(50)))
                {
                    if(tab != 2)
                    {
                        tab = 2;

                        //This Button
                        this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2.png", typeof(Texture));
                        this.buttonTexture_con2 = new GUIContent(buttonTexture2);

                        //Other Buttons
                        this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon_0.png", typeof(Texture));
                        this.buttonTexture_con = new GUIContent(buttonTexture);

                        this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3_0.png", typeof(Texture));
                        this.buttonTexture_con3 = new GUIContent(buttonTexture3);
                    }
                }
                if(GUILayout.Button(buttonTexture_con3, GUILayout.Width((position.width / 3) - 4f), GUILayout.Height(50)))
                {
                    if(tab != 3)
                    {
                        tab = 3;

                        //This Button
                        this.buttonTexture3 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon3.png", typeof(Texture));
                        this.buttonTexture_con3 = new GUIContent(buttonTexture3);

                        //Other Buttons
                        this.buttonTexture = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon_0.png", typeof(Texture));
                        this.buttonTexture_con = new GUIContent(buttonTexture);

                        this.buttonTexture2 = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Strain/Editor/Icons/Tab_icon2_0.png", typeof(Texture));
                        this.buttonTexture_con2 = new GUIContent(buttonTexture2);
                    }
                }
                EditorGUILayout.EndHorizontal();
                

                //Strain Tool Tabs
                //----------------
                //Tab 1
                if(tab == 1)
                {
                    //Tab 1 Title
                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.yellow;
                    EditorGUILayout.LabelField("• Strain Add Properties •", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                    EditorGUILayout.EndVertical();

                    //Tab 1 Properties
                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.white;
                    strainSize = EditorGUILayout.Slider("  Thickness", strainSize, 1f , 100f);
                    strainSegments = EditorGUILayout.IntSlider("  Segments", strainSegments, 2 , 100);
                    strainSegmentsDistance = EditorGUILayout.Slider("  Segment Distance", strainSegmentsDistance, 0.1f , 1f);
                    strainWeight = EditorGUILayout.Slider("  Weight", strainWeight, 0 , 100);
                    if(!physics)
                    {
                        strainStiffness = EditorGUILayout.Slider("  Stiffness", strainStiffness, 100, 50000);
                    }
                    distanceApart = EditorGUILayout.Slider("  Min Draw Distance", distanceApart, 0.1f , 10);
                    strainColor = EditorGUILayout.ColorField("  Color", strainColor);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("  Texture ");
                    strainTexture = (Texture2D)EditorGUILayout.ObjectField(strainTexture, typeof(Texture2D), true);
                    EditorGUILayout.EndHorizontal();
                    strainTexTiling = EditorGUILayout.Vector2Field("  Texture Tiling", strainTexTiling);
                    strainShape = EditorGUILayout.CurveField("  Shape", strainShape);
                    EditorGUILayout.EndVertical();

                    if(!physics)
                    {
                        //Wind properties
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.white;
                        windAnimation = EditorGUILayout.Toggle("  Wind Animation", windAnimation);
                        if(windAnimation)
                        {
                            strainAnimation = EditorGUILayout.CurveField("  Strain Wind Wave", strainAnimation);
                        }
                        EditorGUILayout.EndVertical();
                    }

                    //Physics properties
                    EditorGUILayout.BeginVertical("box");
                    GUI.contentColor = Color.white;
                    physics = EditorGUILayout.Toggle("  Physics Strain", physics);
                    if(physics)
                    {
                        useWindForce = EditorGUILayout.Toggle("  Use Wind Force", useWindForce);

                        if(useWindForce)
                        {
                            windForce = EditorGUILayout.IntSlider("  Wind Force Amount", windForce, 0 , 200);
                            windDirection = EditorGUILayout.Vector3Field("  Wind Direction", windDirection);
                        }

                        GUI.contentColor = Color.grey;
                        EditorGUILayout.LabelField("  WARNING: Physics strains are heavy on performance", EditorStyles.boldLabel);
                        
                        //Create strains layer if none
                        if(!layerExists)
                        {
                            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                            SerializedProperty layersProp = tagManager.FindProperty("layers");

                            SerializedProperty sp;

                            for(int i = 8; i < layersProp.arraySize; i++)
                            {
                                sp = layersProp.GetArrayElementAtIndex(i);

                                if(sp.stringValue == "Strains" && !layerExists)
                                {
                                    layerExists = true;
                                } 

                                else if(sp.stringValue == "" && !layerExists)
                                {
                                    sp.stringValue = strainLayer;
                                    tagManager.ApplyModifiedProperties();
                                    Physics.IgnoreLayerCollision(LayerMask.NameToLayer(sp.stringValue), LayerMask.NameToLayer(sp.stringValue));
                                    layerExists = true;
                                }   
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                }


                    //Tab 2
                    if(tab == 2)
                    {
                        //Tab 2 Title
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.yellow;
                        EditorGUILayout.LabelField("• Strain Remove Properties •", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.EndVertical();

                        //Tab 2 Properties
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.white;
                        removeRadius = EditorGUILayout.Slider("  Remove Radius", removeRadius, 1f , 100f);
                        EditorGUILayout.EndVertical();
                        
                        //Remove all strains
                        EditorGUILayout.BeginVertical("box");
                        if (GUILayout.Button("Remove All Strains"))
                        {
                            //Pop-up so you don't accidentally remove all strains
                            if (EditorUtility.DisplayDialog("Just Making Sure...", "Do you want to remove all strains?", "Yes", "No"))
                            {
                                RemoveAllStrains();
                                MarkSceneAsDirty();
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }


                    //Tab 3
                    if(tab == 3)
                    {
                        //Tab 3 Title
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.yellow;
                        EditorGUILayout.LabelField("• Strain Style Properties •", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                        EditorGUILayout.EndVertical();

                        //Tab 3 Properties
                        EditorGUILayout.BeginVertical("box");
                        GUI.contentColor = Color.white;
                        action = (Actions)EditorGUILayout.EnumPopup("  Styling Method", action);
                        brushSize = EditorGUILayout.Slider("  Brush Size", brushSize, 1f , 100f);
                        if(action == Actions.PushStrain || action == Actions.PullStrain)
                        {
                            brushStrength = EditorGUILayout.Slider("  Brush Strength", brushStrength, 0.01f , 1f);
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.BeginVertical("box");
                            GUI.contentColor = Color.white;
                            styleInBrushDirection = EditorGUILayout.Toggle("  Style In Brush Direction", styleInBrushDirection);
                            if(!styleInBrushDirection)
                            {
                                styleInDirection = EditorGUILayout.Vector3Field("  Style In Direction", styleInDirection);
                            }
                        }
                        
                        else if(action == Actions.GrowStrain || action == Actions.ShrinkStrain)
                        {   
                            if(!segmentDistance)
                            {
                                IntBrushSegments = EditorGUILayout.IntSlider("  Brush Segments", IntBrushSegments, 1 , 10);
                            }
                            
                            else
                            {
                                floatBrushSegments = EditorGUILayout.Slider("  Brush Segment Distance", floatBrushSegments, 0.0005f , 0.001f);
                            }
                            
                            segmentDistance = EditorGUILayout.Toggle("  Segment Distance", segmentDistance);
                        }
                        
                        else if(action == Actions.WeightStrain)
                        {
                            weightStrain = EditorGUILayout.Slider("  Brush Weight", weightStrain, 0f , 100f);
                        }
                        
                        else if(action == Actions.ColorStrain)
                        {
                            colorStrain = EditorGUILayout.ColorField("  Brush Color", colorStrain);
                        }
                        EditorGUILayout.EndVertical();

                    }
                }
                
                
                //Footer Label
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField("");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                GUI.contentColor = Color.grey;
                EditorGUILayout.LabelField("Strain v1.3 - by The Famous Mouse", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter});
                EditorGUILayout.EndHorizontal();
            }
        }
    
    
    /////////////
    //On SceneGUI
    private void OnSceneGUI(SceneView sv)
    {
        if(!Application.isPlaying)
        {
            if(selectedObject && objectToEdit != null && editObject)
            {
                //Raycast from the mouse position
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit))
                {
                    if(hit.transform.gameObject == objectToEdit)
                    {   
                        //Brush position
                        brushPos = hit.point;

                        //Calculate brush angle
                        brushAngle = hit.normal;


                        //Gizmos
                        if(tab == 1)
                        {
                                //Display circle and line
                                Handles.color = Color.magenta;
                                Handles.DrawWireDisc(brushPos, brushAngle, strainSize * 0.01f);
                                Handles.DrawLine(brushPos, brushPos - ((-brushAngle * 1.5f) * 0.1f));
                        }

                        if(tab == 2)
                        {
                                //Display circle
                                Handles.color = Color.red;
                                Handles.DrawWireDisc(brushPos, brushAngle, removeRadius * 0.01f);
                        }

                        if(tab == 3)
                        {
                                //Display circle and line
                                Handles.color = Color.cyan;
                                Handles.DrawWireDisc(brushPos, brushAngle, brushSize * 0.01f);
                                Handles.DrawLine(brushPos, brushPos - ((-brushAngle * 1.5f) * 0.1f));
                        }



                        //First make sure we cant select another gameobject in the scene when we click
                        HandleUtility.AddDefaultControl(0);
                        
                        
                        //Check if we have clicked with the left mouse button
                        Event e = Event.current;
                        
                        if(e.button == 0 && e.isMouse)
                        {
                            if(e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                            {
                                var canDraw = matchingDistance();

                                //Add strains
                                if(tab == 1 && canDraw)
                                {
                                    AddNewStrain();
                                    MarkSceneAsDirty();
                                }

                                //Remove strains
                                else if(tab == 2)
                                {
                                    RemoveStrain();
                                    MarkSceneAsDirty();
                                }

                                //Style strains
                                else if(tab == 3)
                                {
                                    StyleStrain();
                                    MarkSceneAsDirty();
                                }
                            }
                        }
                    }
                }

                sv.Repaint();
            }
        }
    }
    
    //////////////////
    //Update scene GUI
    private void OnInspectorUpdate()
    {
        //Repaint
        Repaint();
    }
    
    //////////////////////////////////////////
    //Check if distance matches distance apart
    private bool matchingDistance() 
    {
        foreach(Transform strain in objectToEdit.transform)
        {
            if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
            {
                if(!strains.Contains(strain.gameObject))
                {
                    strains.Add(strain.gameObject);
                }
            }
        }
        
        for( int i = 0; i < strains.Count; ++i ) 
        {
            if(Vector3.Distance(strains[i].transform.position, brushPos) < distanceApart * 0.1f) 
            {
                return false;
            }
        }
 
        return true;
    }
    
    ///////////////////////////////////////////////
    //Add strains to selected object and set values
    private void AddNewStrain()
    {
        //Dynamic non-physics strains
        if(!physics)
        {
            //Clear list
            strains.Clear();
            
            //Set new strain prefab
            GameObject newStrain = PrefabUtility.InstantiatePrefab(prefabStrain) as GameObject;
            
            //Add new strain to list
            strains.Add(newStrain);
            
            //Set new strain properties
            newStrain.transform.position = brushPos;
            newStrain.transform.rotation = Quaternion.LookRotation(brushAngle, newStrain.transform.forward);

            if(skinnedMesh)
            {
                newStrain.transform.parent = boneReference.transform;
            }
            
            else
            {
                newStrain.transform.parent = objectToEdit.transform;
            }
            
            //Strain shape & size
            strainShape.AddKey(0, strainSize);
            newStrain.GetComponent<LineRenderer>().widthCurve = strainShape;
            newStrain.GetComponent<LineRenderer>().widthMultiplier = strainSize * 0.01f;
            
            //Strain segments
            newStrain.GetComponent<StrainBehavior>().segments = strainSegments;
            
            //Strain segment distance apart
            newStrain.GetComponent<StrainBehavior>().segmentDistance = strainSegmentsDistance * 0.1f;

            //Strain weight
            newStrain.GetComponent<StrainBehavior>().strainWeight = strainWeight;
            
            //Strain stiffness
            newStrain.GetComponent<StrainBehavior>().stiffness = strainStiffness;
            
            //Strain wind
            if(windAnimation)
            {
                newStrain.GetComponent<StrainBehavior>().windAnimation = true;
                newStrain.GetComponent<StrainBehavior>().windCurve = strainAnimation;
            }

            else if(!windAnimation)
            {
                newStrain.GetComponent<StrainBehavior>().windAnimation = false;
            }
            
            //Strain color
            if(GraphicsSettings.renderPipelineAsset == null)
            {

                newStrain.GetComponent<StrainBehavior>().strainColor = strainColor;
                newStrain.GetComponent<LineRenderer>().GetPropertyBlock(newStrain.GetComponent<StrainBehavior>()._propBlock);
                newStrain.GetComponent<StrainBehavior>()._propBlock.SetColor("_Color", strainColor);
                newStrain.GetComponent<LineRenderer>().SetPropertyBlock(newStrain.GetComponent<StrainBehavior>()._propBlock);

            } 

            else
            {

                newStrain.GetComponent<StrainBehavior>().strainColor = strainColor;
                newStrain.GetComponent<LineRenderer>().GetPropertyBlock(newStrain.GetComponent<StrainBehavior>()._propBlock);
                newStrain.GetComponent<StrainBehavior>()._propBlock.SetColor("_BaseColor", strainColor);
                newStrain.GetComponent<LineRenderer>().SetPropertyBlock(newStrain.GetComponent<StrainBehavior>()._propBlock);

            }
            
            if(strainTexture != null)
            {
                newStrain.GetComponent<StrainBehavior>().strainTexture = strainTexture;
                newStrain.GetComponent<StrainBehavior>().strainTexTiling = strainTexTiling;
            }
            
        }
        
        //Physics strains
        else if(physics)
        {
            //Clear list
            strains.Clear();
            
            //Set new strain prefab
            GameObject newStrain = PrefabUtility.InstantiatePrefab(prefabPhysicsStrain) as GameObject;
            
            //Add new strain to list
            strains.Add(newStrain);
            
            //Set new strain properties
            newStrain.transform.position = brushPos;
            newStrain.transform.rotation = Quaternion.LookRotation(brushAngle, newStrain.transform.forward);
            
            if(skinnedMesh)
            {
                newStrain.transform.parent = boneReference.transform;
                newStrain.GetComponent<PhysicsStrainBehavior>().rootReference = boneReference;
            }
            
            else
            {
                newStrain.transform.parent = objectToEdit.transform;
                newStrain.GetComponent<PhysicsStrainBehavior>().rootReference = objectToEdit;
            }
            
            //Strain shape & size
            strainShape.AddKey(0, strainSize);
            newStrain.GetComponent<LineRenderer>().widthCurve = strainShape;
            newStrain.GetComponent<LineRenderer>().widthMultiplier = strainSize * 0.01f;
            newStrain.GetComponent<PhysicsStrainBehavior>().strainThickness = 0.01f;
            
            
            //Strain segments
            newStrain.GetComponent<PhysicsStrainBehavior>().segments = strainSegments;
            
            //Strain segment distance apart
            newStrain.GetComponent<PhysicsStrainBehavior>().segmentDistance = strainSegmentsDistance * 0.1f;

            //Strain weight
            newStrain.GetComponent<PhysicsStrainBehavior>().strainWeight = strainWeight;
            
            //If using wind force, apply it
            if(useWindForce)
            {
                newStrain.GetComponent<PhysicsStrainBehavior>().windForce = windForce;
                newStrain.GetComponent<PhysicsStrainBehavior>().windDirection = windDirection;
            }
            
            //Strain color
            if(GraphicsSettings.renderPipelineAsset == null)
            {

                newStrain.GetComponent<PhysicsStrainBehavior>().strainColor = strainColor;
                newStrain.GetComponent<LineRenderer>().GetPropertyBlock(newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock);
                newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock.SetColor("_Color", strainColor);
                newStrain.GetComponent<LineRenderer>().SetPropertyBlock(newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock);

            } 

            else
            {

                newStrain.GetComponent<PhysicsStrainBehavior>().strainColor = strainColor;
                newStrain.GetComponent<LineRenderer>().GetPropertyBlock(newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock);
                newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock.SetColor("_BaseColor", strainColor);
                newStrain.GetComponent<LineRenderer>().SetPropertyBlock(newStrain.GetComponent<PhysicsStrainBehavior>()._propBlock);

            }
            
            if(strainTexture != null)
            {
                newStrain.GetComponent<PhysicsStrainBehavior>().strainTexture = strainTexture;
                newStrain.GetComponent<PhysicsStrainBehavior>().strainTexTiling = strainTexTiling;
            }
        }
    }
    
    ///////////////////////////////////
    //Remove strains in particular area
    private void RemoveStrain()
    {
        //Add strains to list if none or not recorded
        if(skinnedMesh)
        {
            foreach(Transform strain in boneReference.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
            
        else
        {
            foreach(Transform strain in objectToEdit.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
        
        //Remove within radius
        for(int i = 0; i < strains.Count; i++) 
        {
            if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (removeRadius * 0.01f) * (removeRadius * 0.01f))
            {
                 DestroyImmediate(strains[i]);
                 strains.Remove(strains[i]);
            }
        }
    }
    
    ///////////////////////////////////////
    //Remove all strains on selected object
    private void RemoveAllStrains()
    {
        //Add strains to list if none or not recorded
        if(skinnedMesh)
        {
            foreach(Transform strain in boneReference.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
            
        else
        {
            foreach(Transform strain in objectToEdit.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
        
        //Remove all
        foreach(GameObject strain in strains)
        {
            DestroyImmediate(strain);
        }
        
        //Clear list
        strains.Clear();
    }
    
    ///////////////////////////////////
    //Direct strains in brush direction
    private void StyleStrain()
    {
        //Add strains to list if none or not recorded
        if(skinnedMesh)
        {
            foreach(Transform strain in boneReference.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
            
        else
        {
            foreach(Transform strain in objectToEdit.transform)
            {
                if(strain.gameObject.name == "Strain" || strain.gameObject.name == "PhysicsStrain")
                {
                    if(!strains.Contains(strain.gameObject))
                    {
                        strains.Add(strain.gameObject);
                    }
                }
            }
        }
        
        
        //Style in brush direction
        for(int i = 0; i < strains.Count; i++) 
        {
            if(styleInBrushDirection)
            {   
                //Push strain
                if(action == Actions.PushStrain)
                {
                    if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                    {
                        if(strains[i].GetComponent<StrainBehavior>())
                        {
                            Quaternion newRotation = Quaternion.LookRotation(brushPos - (brushAngle * 10));
                            strains[i].transform.rotation = Quaternion.Slerp(strains[i].transform.rotation, newRotation, Time.deltaTime * (brushStrength * 15));
                        }
                    }
                }
                
                //Pull strain
                else if(action == Actions.PullStrain)
                {
                    if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                    {
                        if(strains[i].GetComponent<StrainBehavior>())
                        {
                            Quaternion newRotation = Quaternion.LookRotation(brushPos - (-brushAngle * 10));
                            strains[i].transform.rotation = Quaternion.Slerp(strains[i].transform.rotation, newRotation, Time.deltaTime * (brushStrength * 15));
                        }
                    }
                }
            }
            
            
            //Style in defined direction
            else if(!styleInBrushDirection)
            {
                if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                {
                    //Push strain
                    if(action == Actions.PushStrain)
                    {
                        if(strains[i].GetComponent<StrainBehavior>())
                        {
                            Quaternion newRotation = Quaternion.LookRotation(styleInDirection);
                            strains[i].transform.rotation = Quaternion.Slerp(strains[i].transform.rotation, newRotation, Time.deltaTime * (brushStrength * 15));
                        }
                    }
                    
                    //Pull strain
                    else if(action == Actions.PullStrain)
                    {
                        if(strains[i].GetComponent<StrainBehavior>())
                        {
                            Quaternion newRotation = Quaternion.LookRotation(-styleInDirection);
                            strains[i].transform.rotation = Quaternion.Slerp(strains[i].transform.rotation, newRotation, Time.deltaTime * (brushStrength * 15));
                        }
                    }
                    
                }
            }
            
               
            //Grow strain
            if(action == Actions.GrowStrain)
            {
                if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                {
                    if(strains[i].GetComponent<StrainBehavior>())
                    {
                        if(!segmentDistance)
                        {
                            strains[i].GetComponent<StrainBehavior>().enabled = false;
                            strains[i].GetComponent<StrainBehavior>().segments += IntBrushSegments;
                            strains[i].GetComponent<StrainBehavior>().enabled = true;
                            strains[i].GetComponent<StrainBehavior>().StrainSegmentSetup();
                        }
                            
                        else if(segmentDistance)
                        {
                            strains[i].GetComponent<StrainBehavior>().enabled = false;
                            strains[i].GetComponent<StrainBehavior>().segmentDistance += floatBrushSegments;
                            strains[i].GetComponent<StrainBehavior>().enabled = true;
                            strains[i].GetComponent<StrainBehavior>().StrainSegmentSetup();
                        }
                    }
                }
            }
                
            //Shrink strain
            else if(action == Actions.ShrinkStrain)
            {
                if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                {
                    if(strains[i].GetComponent<StrainBehavior>())
                    {
                        if(!segmentDistance)
                        {
                            if(strains[i].GetComponent<StrainBehavior>().segments > IntBrushSegments)
                            {
                                strains[i].GetComponent<StrainBehavior>().enabled = false;
                                strains[i].GetComponent<StrainBehavior>().segments -= IntBrushSegments;
                                strains[i].GetComponent<StrainBehavior>().enabled = true;
                                strains[i].GetComponent<StrainBehavior>().StrainSegmentSetup();
                            }
                        }
                            
                        else if(segmentDistance)
                        {
                            if(strains[i].GetComponent<StrainBehavior>().segmentDistance > floatBrushSegments)
                            {
                                strains[i].GetComponent<StrainBehavior>().enabled = false;
                                strains[i].GetComponent<StrainBehavior>().segmentDistance -= floatBrushSegments;
                                strains[i].GetComponent<StrainBehavior>().enabled = true;
                                strains[i].GetComponent<StrainBehavior>().StrainSegmentSetup();
                            }
                        }
                    }
                }
            }
            
            //Weight Strain
            else if(action == Actions.WeightStrain)
            {
                if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                {
                    if(strains[i].GetComponent<StrainBehavior>())
                    {
                        strains[i].GetComponent<StrainBehavior>().enabled = false;
                        strains[i].GetComponent<StrainBehavior>().strainWeight = weightStrain;
                        strains[i].GetComponent<StrainBehavior>().enabled = true;
                    }
                }
            }
            
            //Color Strain
            else if(action == Actions.ColorStrain)
            {
                if(Vector3.SqrMagnitude(strains[i].transform.position - brushPos) < (brushSize * 0.01f) * (brushSize * 0.01f))
                {
                    if(strains[i].GetComponent<StrainBehavior>())
                    {
                        strains[i].GetComponent<StrainBehavior>().enabled = false;
                        strains[i].GetComponent<StrainBehavior>().strainColor = colorStrain;
                        strains[i].GetComponent<StrainBehavior>().enabled = true;
                        strains[i].GetComponent<StrainBehavior>().StrainColorSetup();
                    }
                    
                    if(strains[i].GetComponent<PhysicsStrainBehavior>())
                    {
                        strains[i].GetComponent<PhysicsStrainBehavior>().enabled = false;
                        strains[i].GetComponent<PhysicsStrainBehavior>().strainColor = colorStrain;
                        strains[i].GetComponent<PhysicsStrainBehavior>().enabled = true;
                        strains[i].GetComponent<PhysicsStrainBehavior>().StrainColorSetup();
                    }
                }
            }
        }
    }
    
    //Reset tool data
    private void ResetTool()
    {
        objectToEdit = null;
        boneReference = null;
        selectedObject = false;
        editObject = false;
        tab = 1;
        skinnedMesh = false;
        Tools.hidden = false;

        if(tempMesh != null)
        {
            DestroyImmediate(tempMesh);
        }
                
        if(bakedMesh != null)
        {
            DestroyImmediate(bakedMesh);
        }
    }
    
    //Set simulate in editor
    public bool simulateStrainsInEditor
    {
        get { return simulateStrains ; }
        set
        {
            if( value == simulateStrains )
                return ;

            simulateStrains = value;
            
            if(!simulateStrains)
            {
                var sceneStrains = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Strain");
                foreach(GameObject allStrains in sceneStrains)
                {
                    allStrains.GetComponent<StrainBehavior>().simulateInEditor = false;
                }
                        
            }
                
            else if(simulateStrains)
            {
                var sceneStrains = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Strain");
                foreach(GameObject allStrains in sceneStrains)
                {
                    allStrains.GetComponent<StrainBehavior>().simulateInEditor = true;
                }
            }   
        }    
    }
    
    /////////////////////////////
    //Mark scene for save pending
    private void MarkSceneAsDirty()
    {
        UnityEngine.SceneManagement.Scene activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
    }
}

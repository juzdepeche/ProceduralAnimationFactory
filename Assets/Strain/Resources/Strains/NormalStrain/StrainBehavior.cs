//-------------------------------
//--- Strain - Stylized Hair Tool
//--- Version 1.3
//--- © The Famous Mouse™
//-------------------------------

using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class StrainBehavior : MonoBehaviour
{
    //Public Variables
    //////////////////
    
    public int segments;
    public float segmentDistance = 0.01f, stiffness = 1500f,
    strainWeight = 0f;
    
    public bool windAnimation = false;
    public AnimationCurve windCurve = new AnimationCurve(new Keyframe(0, 0.25f), new Keyframe(1, 0.2f));
    public Color strainColor;
    public Texture2D strainTexture;
    public Vector2 strainTexTiling;
    public bool simulateInEditor = true;
    
    
    //Hidden Variables
    //////////////////
    
    [HideInInspector]
    public LineRenderer strain;
    
    [HideInInspector]
    public MaterialPropertyBlock _propBlock;
    
    Vector3[] segmentPositions;
    Vector3[] segmentVelocity;
    Vector3 refGravity;
    
    bool initializedForEditor = false;
    
    
    //-----------------------------
    
    
    private void Awake()
    {
        //Set variables
        strain = this.gameObject.GetComponent<LineRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }
    
    
    //Initialize
    private void Start()
    {
        //Setup strain properties
        strain.positionCount = segments;
        segmentPositions = new Vector3[segments];
        segmentVelocity = new Vector3[segments];
        
        for(int i = 0; i < segmentPositions.Length; i++)
        {
            segmentPositions[i] = transform.position;
            strain.SetPositions(segmentPositions);
        }
        
        if(GraphicsSettings.currentRenderPipeline)
        {
            if(GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
            {
                strain.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", strainColor);

                if(strainTexture != null)
                {
                    _propBlock.SetTexture("_BaseColorMap", strainTexture);   
                    _propBlock.SetVector("_BaseColorMap_ST", strainTexTiling);
                }

                strain.SetPropertyBlock(_propBlock); 
            }
            else
            {
                strain.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", strainColor);

                if(strainTexture != null)
                {
                    _propBlock.SetTexture("_BaseMap", strainTexture);
                    _propBlock.SetVector("_BaseMap_ST", strainTexTiling);
                }

                strain.SetPropertyBlock(_propBlock);
            }
        }
        
        else
        {
            strain.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", strainColor);
            
            if(strainTexture != null)
            {
                _propBlock.SetTexture("_MainTex", strainTexture);
                _propBlock.SetVector("_MainTex_ST", strainTexTiling); 
            }
            
            strain.SetPropertyBlock(_propBlock);
        }
        
        windCurve.preWrapMode = WrapMode.PingPong;
        windCurve.postWrapMode = WrapMode.PingPong;
        
        initializedForEditor = true;
    }
    
    //Runtime
    private void Update()
    {
        if(Application.isPlaying)
        {            
            if(strain.positionCount != 0)
            {
                //Update strain segment positions with wind unchecked
                if(!windAnimation)
                {
                    segmentPositions[0] = transform.position;
                    refGravity = new Vector3(0, strainWeight, 0);

                    for(int i = 1; i < segmentPositions.Length; i++)
                    {
                         Vector3 targetPos = segmentPositions[i - 1] + (segmentPositions[i] - segmentPositions[i - 1]).normalized * segmentDistance + transform.forward * segmentDistance - (new Vector3(0, i / (1 / strainWeight * 10000f), 0));
                         segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPos, ref segmentVelocity[i], i / stiffness);
                    }

                    strain.SetPositions(segmentPositions);
                }

                //Update strain segment positions with wind checked
                else if(windAnimation)
                {
                    segmentPositions[0] = transform.position;
                    refGravity = new Vector3(0, strainWeight, 0);

                    for(int i = 1; i < segmentPositions.Length; i++)
                    {
                         Vector3 targetPos = segmentPositions[i - 1] + (segmentPositions[i] - segmentPositions[i - 1]).normalized * segmentDistance + transform.forward * segmentDistance - (new Vector3(0, i / (1 / strainWeight * 10000f) * windCurve.Evaluate(Time.time), 0));
                        segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPos, ref segmentVelocity[i], i / stiffness);
                    }

                    strain.SetPositions(segmentPositions);
                }
            }
        }
    }
    
    //Editor
    private void OnDrawGizmos()
    {
        #if UNITY_EDITOR
            
        if(!Application.isPlaying && simulateInEditor)
        {
            if(initializedForEditor)
            {
                if(strain.positionCount != 0 && segmentPositions.Length != 0)
                {
                    //Update strain segment positions without wind
                    if(!windAnimation)
                    {
                        segmentPositions[0] = transform.position;
                        refGravity = new Vector3(0, strainWeight, 0);

                        for(int i = 1; i < segmentPositions.Length; i++)
                        {
                            Vector3 targetPos = segmentPositions[i - 1] + (segmentPositions[i] - segmentPositions[i - 1]).normalized * segmentDistance + transform.forward * segmentDistance - (new Vector3(0, i / (1 / strainWeight * 10000f), 0));
                            segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPos, ref segmentVelocity[i], i / stiffness);
                        }

                        strain.SetPositions(segmentPositions);
                    }

                    //Update strain segment positions with wind
                    else if(windAnimation)
                    {
                        segmentPositions[0] = transform.position;
                         refGravity = new Vector3(0, strainWeight, 0);

                        for(int i = 1; i < segmentPositions.Length; i++)
                        {
                            Vector3 targetPos = segmentPositions[i - 1] + (segmentPositions[i] - segmentPositions[i - 1]).normalized * segmentDistance + transform.forward * segmentDistance - (new Vector3(0, i / (1 / strainWeight * 10000f) * windCurve.Evaluate(Time.time), 0));
                             segmentPositions[i] = Vector3.SmoothDamp(segmentPositions[i], targetPos, ref segmentVelocity[i], i / stiffness);
                        }

                        strain.SetPositions(segmentPositions);
                    }
                    

                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }
        
        #endif
    }
    
    //Public function re-setup for editor styling changes
    public void StrainSegmentSetup()
    {
        //Setup strain properties
        strain.positionCount = segments;
        segmentPositions = new Vector3[segments];
        segmentVelocity = new Vector3[segments];
        
        for(int i = 0; i < segmentPositions.Length; i++)
        {
            segmentPositions[i] = transform.position;
            strain.SetPositions(segmentPositions);
        }
    }
    
    public void StrainColorSetup()
    {
        _propBlock = new MaterialPropertyBlock();
        
        if(GraphicsSettings.currentRenderPipeline)
        {
            if(GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
            {
                strain.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", strainColor);

                if(strainTexture != null)
                {
                    _propBlock.SetTexture("_BaseColorMap", strainTexture);   
                    _propBlock.SetVector("_BaseColorMap_ST", strainTexTiling);
                }

                strain.SetPropertyBlock(_propBlock); 
            }
            else
            {
                strain.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", strainColor);

                if(strainTexture != null)
                {
                    _propBlock.SetTexture("_BaseMap", strainTexture);
                    _propBlock.SetVector("_BaseMap_ST", strainTexTiling);
                }

                strain.SetPropertyBlock(_propBlock);
            }
        }
        
        else
        {
            strain.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", strainColor);
            
            if(strainTexture != null)
            {
                _propBlock.SetTexture("_MainTex", strainTexture);
                _propBlock.SetVector("_MainTex_ST", strainTexTiling); 
            }
            
            strain.SetPropertyBlock(_propBlock);
        }
    }
}

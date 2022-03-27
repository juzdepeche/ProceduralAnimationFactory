//-------------------------------
//--- Strain - Stylized Hair Tool
//--- Version 1.3
//--- © The Famous Mouse™
//-------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class PhysicsStrainBehavior : MonoBehaviour
{
    //Public variables
    //////////////////
    
    public GameObject physicsPrefab;
    
    public int segments, windForce = 0;
    public float segmentDistance = 0.01f, strainWeight = 0f;
    public Vector3 windDirection;
    
    public Color strainColor;
    public Texture2D strainTexture;
    public Vector2 strainTexTiling;
    
    //Hidden Variables
    //////////////////
    
    [HideInInspector]
    public LineRenderer strain;
    
    Vector3[] segmentPositions;
    
    [HideInInspector]
    public MaterialPropertyBlock _propBlock;
    
    [HideInInspector]
    public List<GameObject> physicsStrains;
    
    [HideInInspector]
    public List<Rigidbody> strainRbs;
    
    [HideInInspector]
    public GameObject rootReference;
    
    [HideInInspector]
    public float strainThickness;
    
    ConfigurableJoint rootJoint;
    
    [HideInInspector]
    public bool useConnectedBody = false,
    initializedSetup = false;
    
    
    //------------------------------
    
    
    private void Awake()
    {
        //Set variables
        strain = this.gameObject.GetComponent<LineRenderer>();
        _propBlock = new MaterialPropertyBlock();
        
        if(Time.fixedDeltaTime != 0.002f)
        {
            Time.fixedDeltaTime = 0.002f;
        }
    }
    
    private void Start()
    {
        //Setup strain properties
        strain.positionCount = segments;
        segmentPositions = new Vector3[segments];
        
        for(int i = 0; i < segmentPositions.Length; i++)
        {
            strain.SetPosition(i, transform.position);
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
        
        //Initialize
        if(Application.isPlaying)
        {
            if(!initializedSetup)
            {
                physicsStrains = new List<GameObject>();
                strainRbs = new List<Rigidbody>();
                
                //Setup physics bodies
                for(int i = 0; i < segmentPositions.Length; i++)
                {
                    physicsStrains.Add(Instantiate(physicsPrefab, transform.position, transform.rotation));
                    
                    var layerCheck = LayerMask.NameToLayer("Strains");
                    if(layerCheck > -1)
                    {
                        physicsStrains[i].layer = LayerMask.NameToLayer("Strains");
                    }
                    
                    physicsStrains[i].transform.parent = transform;
                    strainRbs.Add(physicsStrains[i].GetComponent<Rigidbody>());
                    physicsStrains[i].GetComponent<SphereCollider>().radius = strainThickness + (segmentDistance / 2);
                    
                    if(strainWeight < 1)
                    {
                        strainRbs[i].mass = 1;
                    }
                    else
                    {
                        strainRbs[i].mass = strainWeight;
                    }
                    
                    if(i != 0)
                    {
                        physicsStrains[i].transform.position = physicsStrains[i - 1].transform.position + (transform.forward * segmentDistance);
                    }
                }
                
                //Check root reference for rigidbody
                for(int i = segmentPositions.Length - 1; i > -1; i--)
                {
                    if(i == 0)
                    {
                        if(rootReference.GetComponent<Rigidbody>())
                        {
                            physicsStrains[i].GetComponent<ConfigurableJoint>().connectedBody = rootReference.GetComponent<Rigidbody>();
                            useConnectedBody = true;
                        }
                        
                        else
                        {
                            rootJoint = physicsStrains[i].GetComponent<ConfigurableJoint>();
                            rootJoint.autoConfigureConnectedAnchor = false;
                        }
                    }

                    else
                    {
                        physicsStrains[i].GetComponent<ConfigurableJoint>().connectedBody = physicsStrains[i - 1].GetComponent<Rigidbody>();
                    }

                }

                initializedSetup = true;
            }
        }
        
    }
    
    //Runtime
    private void Update()
    {
        if(Application.isPlaying)
        {
            if(initializedSetup)
            {
                //Update root joint to drawn position if no rigidbody found and connected body not set
                if(!useConnectedBody)
                {
                    rootJoint.connectedAnchor = Vector3.Lerp(rootJoint.connectedAnchor, transform.position, 100 * Time.deltaTime);
                }
                
                //Update strain segment positions
                for(int i = 0; i < segmentPositions.Length; i++)
                {
                    segmentPositions[i] = physicsStrains[i].transform.position;
                }
                
                //Apply force if wind property checked
                if(windForce > 0)
                {
                    for(int i = 0; i < segmentPositions.Length; i++)
                    {
                        strainRbs[i].AddForce((windDirection - transform.position).normalized * (windForce / 10), ForceMode.Acceleration);
                    }
                }
                
                //Set segments
                strain.SetPositions(segmentPositions);
            }
        }
    }
    
    //Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, strain.widthMultiplier);
    }
    
    //Public function re-setup for editor styling changes
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

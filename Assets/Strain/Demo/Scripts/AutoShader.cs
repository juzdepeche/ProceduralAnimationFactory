using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class AutoShader : MonoBehaviour
{
    Material dummyMaterial, strainMaterial;
    Shader URP, HDRP;
    
    void OnEnable()
    {
        if(GraphicsSettings.renderPipelineAsset != null)
        {
            dummyMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/Strain/Demo/Materials/DummyMat.mat", typeof(Material));
            strainMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/Strain/Resources/Materials/StrainMat.mat", typeof(Material));
            
            URP = Shader.Find("Universal Render Pipeline/Simple Lit");
            HDRP = Shader.Find("HDRP/Lit");
            
            if(GraphicsSettings.currentRenderPipeline)
            {
               if(GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
               {
                  dummyMaterial.shader = HDRP;
                  strainMaterial.shader = HDRP;
                  dummyMaterial.SetColor("_BaseColor", new Color(255f/255f, 133f/255f, 0f/255f));
               }
               else
               {
                  dummyMaterial.shader = URP;
                  strainMaterial.shader = URP;
                  dummyMaterial.SetColor("_BaseColor", new Color(255f/255f, 133f/255f, 0f/255f));
               }
            }
        }
    }
}

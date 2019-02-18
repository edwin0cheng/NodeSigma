using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

using NodeSigma.RenderPass.Runtime;

namespace NodeSigma.RenderPass.Editor
{

[CustomEditor(typeof(DepthNormalsRenderPass))]
public class DephtNormalsRenderPassEditor : UnityEditor.Editor 
{

     private static readonly string[] _dontIncludeMe = new string[]{"m_Script"};
    
    void OnEnable()
    {
        RenderPipeline.beginFrameRendering += RegisterRenderPass;
    }

    void OnDisable()
    {
        RenderPipeline.beginFrameRendering -= RegisterRenderPass;
    }

    public override void OnInspectorGUI()
    {
        // serializedObject.Update();
 
        // // DrawPropertiesExcluding(serializedObject, _dontIncludeMe);
 
        // serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
    DepthNormalsRenderPass GetMainCameraPass()
    {
        var go = GameObject.FindGameObjectWithTag("MainCamera");

        if(go == null) return null;
        return go.GetComponent<DepthNormalsRenderPass>();        
    }

    void UnregisterRenderPass(Camera[] cameras)
    {
        foreach (var c in cameras)
        {
            var pass = c.GetComponent<DepthNormalsRenderPass>();
            if(pass && pass.IsEditorOnly)
            {
                DestroyImmediate(pass);
            }                
        }
    }

    void RegisterRenderPass(Camera[] cameras)
    {
        var pass = GetMainCameraPass();

        if(pass == null || !pass.enabled)
        {
            UnregisterRenderPass(cameras);
            return;
        }

        foreach (var c in cameras)
        {
            var cameraPass = c.GetComponent<DepthNormalsRenderPass>();
            if(cameraPass == null && cameraPass != pass)
            {
                var otherPass = c.gameObject.AddComponent<DepthNormalsRenderPass>();
                otherPass.renderLayerMask = pass.renderLayerMask;
                otherPass.IsEditorOnly = true;                    
            }                
        }
    }
}
   

}
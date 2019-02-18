using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;


namespace NodeSigma.RenderPass.Runtime
{
    // TODO(edwin)
    // In theory we should use IBeforeRender because it will works in Preview Camera
    // However, i tried a lot of things and it still does not work.
    // 
    // On the other hand, the render pass would not work in ShaderGraph Preview
    // because the current implementation of ShaderGraph use `m_SceneResources.camera.Render()` directly.
    // (See PreviewManager::RenderPreview in SRP github)
    // It is in the Unity C++ side which i don't know any render pass would affect it.
    [ExecuteInEditMode]
    [AddComponentMenu("NodeSigma/Render Pass/Depth Normals Render Pass")]
    public class DepthNormalsRenderPass : MonoBehaviour, IAfterDepthPrePass //IBeforeRender //,
    {
        public LayerMask renderLayerMask;
        private DepthNormalsRenderPassImpl pass;
        
        [System.NonSerialized]
        public bool IsEditorOnly = false;

        // public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle, RenderTargetHandle depthHandle, ClearFlag clearFlag)
        public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
        {            
            if (pass == null) pass = new DepthNormalsRenderPassImpl(baseDescriptor, renderLayerMask);

            pass.isEnabled = this.enabled;

            return pass;
        }

        void OnValidate()
        {
            if(pass != null) {
                pass.SetLayerMask(renderLayerMask);
            }
        }

        void Start()
        {

        }
    }

    public class DepthNormalsRenderPassImpl : ScriptableRenderPass
    {
        private const string k_DepthNormals = "Depth Normals";

        private Material m_DepthNormalsMaterial;

        private RenderTextureDescriptor m_baseDescriptor;
        private RenderTargetHandle m_PerObjectRenderTextureHandle;
        private FilterRenderersSettings m_PerObjectFilterSettings;

        internal bool isEnabled = true;

        public DepthNormalsRenderPassImpl(RenderTextureDescriptor baseDescriptor, LayerMask renderLayerMask)
        {
            // All shaders with this lightmode will be in this pass
            RegisterShaderPassName("LightweightForward");

            m_baseDescriptor = new RenderTextureDescriptor(baseDescriptor.width, baseDescriptor.height, RenderTextureFormat.ARGB32, 16);

            // This just writes black values for anything that is rendered
            m_DepthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");

            // Setup a target RT handle (it just wraps the int id)
            m_PerObjectRenderTextureHandle = new RenderTargetHandle();
            m_PerObjectRenderTextureHandle.Init(k_DepthNormals);

            m_PerObjectFilterSettings = new FilterRenderersSettings(true)
            {
                // Render all opaque objects
                renderQueueRange = RenderQueueRange.opaque,
                // Filter further by any renderer tagged
                layerMask = renderLayerMask
            };
        }

        public void SetLayerMask(LayerMask mask)
        {
            m_PerObjectFilterSettings.layerMask = mask;
        }

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(!isEnabled) return;

            CommandBuffer cmd = CommandBufferPool.Get(k_DepthNormals);
            using (new ProfilingSample(cmd, k_DepthNormals))
            {
                var desc = m_baseDescriptor;

                cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, desc);
                SetRenderTarget(
                    cmd,
                    m_PerObjectRenderTextureHandle.Identifier(),
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.DontCare,
                    ClearFlag.All,
                    Color.black, // Clear to white, the stencil writes black values
                    m_baseDescriptor.dimension // Create a buffer the same size as the color buffer
                    );

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var camera = renderingData.cameraData.camera;

                // We want the same rendering result as the main opaque render
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

                // Setup render data from camera
                var drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None,
                    renderingData.supportsDynamicBatching);

                // Everything gets drawn with the stencil shader
                drawSettings.SetOverrideMaterial(m_DepthNormalsMaterial, 0);

                context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_PerObjectFilterSettings);

                // Set a global texture id so we can access this later on
                cmd.SetGlobalTexture("_CameraDepthNormalsTexture", m_PerObjectRenderTextureHandle.id);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);

            // When rendering is done, clean up our temp RT
            cmd.ReleaseTemporaryRT(m_PerObjectRenderTextureHandle.id);
        }
    }

}
#if UNITY_PIPELINE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    [ExecuteInEditMode]
    public class InjectPathTracingPass : MonoBehaviour
    {
        public URPTTPass m_PathTracingPass = null;

        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += InjectPass;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= InjectPass;
        }

        private void CreateRenderPass() {
            m_PathTracingPass = new URPTTPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }
        private void InjectPass(ScriptableRenderContext renderContext, Camera currCamera) {
            if (m_PathTracingPass == null) CreateRenderPass();

            if (Application.isPlaying) {
                currCamera.depthTextureMode |= (DepthTextureMode.MotionVectors | DepthTextureMode.Depth);
                var data = currCamera.GetUniversalAdditionalCameraData();
                m_PathTracingPass.SetTarget(data.scriptableRenderer);
                data.scriptableRenderer.EnqueuePass(m_PathTracingPass);
                currCamera.forceIntoRenderTexture = true;
            }
        }
    }
}
#endif